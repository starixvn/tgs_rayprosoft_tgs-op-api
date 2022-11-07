using System.Net;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Payment.AlfaPay;
using Tgs.Payment.Help2Pay;
using Tgs.Payment.LeePay;
using Tgs.Payment.MalakiPay;
using Tgs.Payment.MaticPay;
using Tgs.Payment.PrimePay;
using Tgs.Payment.SkyPay;
using Tgs.Payment.TuPay;
using Tgs.Payment.UmPay;
using Tgs.Payment.ZeroNineNinePay;
using Tgs.PlayerBucket.Clients.DependencyInjection;
using TGS.Apis.OperatorApi;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Filters;
using TGS.Apis.OperatorApi.Filters.Swaggers;
using TGS.Apis.OperatorApi.Models;
using TGS.Core.DataServices.Tgs.Extensions;
using TGS.Core.Infrastructures.ApiClient;
using TGS.Core.Infrastructures.ErrorMappers;
using TGS.Core.Infrastructures.Mail;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Infrastructures.Transactions;
using TGS.Core.Infrastructures.WebApiServices;
using TGS.Payment.ManualDeposit;
using TGS.Services.Operations.Extensions;
using TGS.Services.Operator;
using TGS.Services.Operator.MessageQueue;
using TGS.Services.Operator.ValidateCode;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
ValidatorOptions.Global.LanguageManager.Enabled = false;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("/config/appsettings.json", true)
	.AddEnvironmentVariables();

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
			fluentOptions.UseMessagePack();
		});
	});

builder.Services
	.AddOptions<ForwardedHeadersOptions>()
	.Configure<IConfiguration>((options, config) =>
	{
		options.ForwardedHeaders = ForwardedHeaders.All;
		options.ForwardLimit = config.GetValue<int>("ForwardedHeaders:ForwardLimit");

		var forwardedForHeaderName = config.GetValue<string>("ForwardedHeaders:ForwardedForHeaderName");
		if (!string.IsNullOrWhiteSpace(forwardedForHeaderName))
			options.ForwardedForHeaderName = forwardedForHeaderName;

		var knownProxies = config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();

		foreach (var knownProxie in knownProxies)
			options.KnownProxies.Add(IPAddress.Parse(knownProxie));

		var knownNetworks = config.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();

		foreach (var cidr in knownNetworks)
		{
			(var ip, var range) = cidr.Split("/");

			options.KnownNetworks.Add(
				new IPNetwork(
					IPAddress.Parse(ip),
					string.IsNullOrEmpty(range)
						? 32
						: int.Parse(range)));
		}
	});

builder.Services
	.AddTgsRepository(builder.Configuration, "Tgs");

builder.Services
	.AddMemoryCache()
	.AddWebApiServices()
	.AddTgsLog()
	.AddSystemJsonSerializer()
	.AddSecurity()
	.AddOperations()
	.AddApiClient()
	.AddReportClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Report:Url");
	})
	.AddPromotionClientServices((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		config.GetSection("Promotion").Bind(options);
	})
	.AddTransactions()
	.AddBrandCertificateClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BrandCertificateBaseUrl = config.GetValue<Uri>("Operator:BrandCertificateBaseUrl");
	})
	.AddBrandServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BrandCertificateBaseUrl = config.GetValue<Uri>("Operator:BrandCertificateBaseUrl");
	})
	.AddGameProvidersPluggableServerClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Operator:GameProvidersPluggableBaseUrl");
	})
	.AddPlayerServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Operator:PlayerServiceBaseUrl");
	})
	.AddDepositAccumulateRouletteClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Operator:DepositAccumulateRouletteServiceBaseUrl");
	});

builder.Services
	.AddRedis((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.ConnectionString = config.GetConnectionString("RedisConfigurationString");
	});

builder.Services
	.AddErrorMapper()
	.AddErrorMapper<OperatorApiErrorMapper>();

builder.Services
	.AddPlayerBucketClient()
	.AddOperatorApiCore()
	.ConfigureOperatorOptions<IConfiguration>((options, config) => config
		.GetSection("Operator").Bind(options))
	.UseOperatorRepositories(sp
		=> sp.GetRequiredService<IRedisConnectionFactory>().GetConnection().GetDatabase())
	.ConfigureWallet((options, sp) =>
	{
		var operatorOptions = sp.GetRequiredService<IOptions<OperatorOptions>>().Value;

		options.BaseUrl = operatorOptions.CashBaseUrl;
	})
	.AddPaymentServiceClient(httpClientBuilder => httpClientBuilder
		.ConfigureHttpClient((sp, http) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();

			http.BaseAddress = config.GetValue<Uri>("Operator:PaymentServiceBaseUrl");
		}))
	.Services
	.AddSingleton<ManualDepositSerializer>()
	.AddSingleton<PrimePaySerializer>()
	.AddSingleton<LeePaySerializer>()
	.AddSingleton<UmPaySerializer>()
	.AddSingleton<MaticPaySerializer>()
	.AddSingleton<Help2PaySerializer>()
	.AddSingleton<SkyPaySerializer>()
	.AddSingleton<ZeroNineNinePaySerializer>()
	.AddSingleton<TuPaySerializer>()
	.AddSingleton<MalakiPaySerializer>()
	.AddSingleton<AlfaPaySerializer>();

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();

			options.Url = config.GetValue<string>("NatsSetting:Urls");
		})
		.AddHandler(
			ProcessorTypes.BrandFrontendInfoChangedEventHandlerType,
			"tgs.brand.frontendinfo.changed"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddOptions<ConstValidateCodeOptions>()
	.Configure<IConfiguration>((options, config) => config
		.GetSection("Operator").Bind(options))
	.Services
	.Decorate<IValidateCodeGenerator, ConstValidateCodeGenerator>();

builder.Services
	.AddIdentityVerifiers();

builder.Services
	.AddRouting(options => options.LowercaseUrls = true);

builder.Services
	.AddControllers(options =>
	{
		options.AddBrandCertificateFilters();
		_ = options.Filters.Add<CheckPlayerEnabledFilter>();
		_ = options.Filters.Add<ValidateModelFilter>();
		options.AddWebApiToolkitFilters();
		_ = options.Filters.Add<TgsResponseWrapperAttribute>();
		_ = options.Filters.Add<ErrorMessageMapperFilter>();
	})
	.ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
	.AddFluentValidation(c =>
	{
		_ = c.RegisterValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
		c.DisableDataAnnotationsValidation = true;
	})
	.AddJsonOptions(jsonOptions => jsonOptions
		.JsonSerializerOptions.Converters.Add(new DateOnlyConverter()));

builder.Services.AddHttpClient<IEmailClient, EmailClient>((sp, c) =>
{
	var url = builder.Configuration.GetSection("Email:Url");
	c.BaseAddress = new Uri(url.Value);
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddHealthChecks();

builder.Services
	.AddOptions<OtlpExporterOptions>()
	.Configure<IConfiguration>((options, conifg) =>
	{
		var nodeIp = conifg.GetValue<string>("NODE_IP");

		if (!string.IsNullOrWhiteSpace(nodeIp))
			options.Endpoint = new Uri($"http://{nodeIp}:4317");

		conifg.GetSection("Opentelemetry:Tracing").Bind(options);
	})
	.Services
	.AddOpenTelemetryTracing(
		builder => builder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddAspNetCoreInstrumentation()
			.AddHttpClientInstrumentation()
			.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
			.AddProfileViewExporter()
			.AddOtlpExporter())
	.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/health")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !httpContext.Request.Path.StartsWithSegments("/metrics")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
	.AddProfileViewer()
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

builder.Services
	.AddSwaggerGen(c =>
	{
		c.SwaggerDoc("v1",
			new OpenApiInfo
			{
				Title = "TGS Operator API",
				Version = "v1"
			});

		c.AddBrandCertificateSecurityDefinition();
		c.OperationFilter<SwaggerHeaderFilter>();
		c.SchemaFilter<SwaggerEnumDescriptionFilter>();
		c.OperationFilter<SwaggerTgsResponseWrapperFilter>();

		var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
		var path = Path.Combine(AppContext.BaseDirectory, xml);

		if (File.Exists(path))
		{
			c.IncludeXmlComments(path);
		}
	});
builder.Services
	.AddCors(options =>
	{
		var corsDomain = builder.Configuration.GetSection("CorsDomain").Get<CorsDomain>();

		options.AddDefaultPolicy(
			builder => builder
				.WithOrigins(corsDomain.Urls)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.SetIsOriginAllowed(_ => true));
	});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRequestLocalization(options =>
{
	var config = app.Services.GetRequiredService<IConfiguration>();

	options.SupportedCultures.Clear();
	options.SupportedUICultures.Clear();

	config.GetSection("RequestLocalization").Bind(options);

	_ = options.SetDefaultCulture(config.GetValue<string>("RequestLocalization:DefaultRequestCulture"));
});

app.UseBrandCertificate();
app.UseRouting();
app.UseCors();
app.UseVerifyJwtTokenMiddlewares(Consts.DefaultBypassRouting);

app.MapControllers();

app.MapHealthChecks("/health");
app.MapProfileViewer();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TGS Operator API"));

app.Run();
