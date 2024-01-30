using System.Net;
using System.Reflection;
using FluentdForward.OpenTelemetry.MessagePack;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NATS.Client.Core;
using OpenTelemetry.Metrics;
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
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client.DependencyInjection;
using TGS.Apis.OperatorApi;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Filters;
using TGS.Apis.OperatorApi.Filters.Swaggers;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.SignalR;
using TGS.Core.DataServices.Tgs.Extensions;
using TGS.Core.Infrastructures.ErrorMappers;
using TGS.Core.Infrastructures.Mail;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Infrastructures.WebApiServices;
using TGS.Payment.ManualDeposit;
using TGS.Services.Operations.Extensions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using TGS.Services.Operator.MessageQueue;
using TGS.Services.Operator.ValidateCode;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
ValidatorOptions.Global.LanguageManager.Enabled = false;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineName",
				() => Environment.MachineName));
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

		var knownProxies = config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

		foreach (var knownProxie in knownProxies)
			options.KnownProxies.Add(IPAddress.Parse(knownProxie));

		var knownNetworks = config.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];

		foreach (var cidr in knownNetworks)
		{
			(var ip, var range) = cidr.Split("/");

			options.KnownNetworks.Add(
				new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
					IPAddress.Parse(ip),
					string.IsNullOrEmpty(range)
						? 32
						: int.Parse(range)));
		}
	});

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie()
	.Services
	.AddHttpContextAccessor()
	.AddRouting(options => options.LowercaseUrls = true)
	.AddSignalR()
	.Services
	.AddControllers(options =>
	{
		options.AddBrandCertificateFilters();
		_ = options.Filters.Add<CheckPlayerEnabledFilter>();
		_ = options.Filters.Add<ValidateModelFilter>();
		options.AddWebApiToolkitFilters();
		_ = options.Filters.Add<TgsResponseWrapperAttribute>();
		_ = options.Filters.Add<PublishPlayerLoggedInEventFilter>();
		_ = options.Filters.Add<ErrorMessageMapperFilter>();
	})
	.ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
	.AddJsonOptions(jsonOptions => jsonOptions
		.JsonSerializerOptions.Converters.Add(new DateOnlyConverter()));

builder.Services
	.AddFluentValidationAutoValidation(c =>
		c.DisableDataAnnotationsValidation = true)
	.AddFluentValidationClientsideAdapters()
	.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);

builder.Services
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
	.AddTransient<IGetService<GetSM01HtmlContentQuery, SM01HtmlContentInfo>, GetSM01HtmlContentQueryService>()
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
	.AddSingleton<AlfaPaySerializer>()
	.AddTgsRepository(builder.Configuration, "Tgs")
	.AddMemoryCache()
	.AddWebApiServices()
	.AddSystemJsonSerializer()
	.AddSecurity()
	.AddOperations()
	.AddReportClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Report:Url")!;
	})
	.AddPromotionClientServices((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		config.GetSection("Promotion").Bind(options);
	})
	.AddBrandCertificateClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BrandCertificateBaseUrl = config.GetValue<Uri>("Operator:BrandCertificateBaseUrl")!;
	})
	.AddBrandServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BrandCertificateBaseUrl = config.GetValue<Uri>("Operator:BrandCertificateBaseUrl")!;
	})
	.AddGameProvidersPluggableServerClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Operator:GameProvidersPluggableBaseUrl")!;
	})
	.AddPlayerServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Operator:PlayerServiceBaseUrl")!;
	})
	.AddDepositAccumulateRouletteClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();

		options.BaseUrl = config.GetValue<Uri>("Operator:DepositAccumulateRouletteServiceBaseUrl")!;
	})
	.AddPlayerBucketClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("Operator:PlayerBucketBaseUrl")!))
	.AddPlayerBucketTypeClient()
	.AddStationLetterClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("Operator:StationLetterBaseUrl")))
	.AddArticleManagementClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("Operator:ArticleManagementBaseUrl")!))
	.AddIdentityVerifiers()
	.AddVipLevelServiceClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
			builder.Configuration.GetValue<Uri>("Operator:VipLevelServiceBaseUrl")!))
	.AddBrandGameCreditsClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("Operator:BrandGameCreditsBaseUrl");
	})
	.AddHttpClient<IEmailClient, EmailClient>((sp, c) =>
	{
		var url = builder.Configuration.GetSection("Email:Url");
		c.BaseAddress = new Uri(url.Value);
	})
	.Services
	.AddOptions<ConstValidateCodeOptions>()
	.Configure<IConfiguration>((options, config) => config
		.GetSection("Operator").Bind(options))
	.Services
	.Decorate<IValidateCodeGenerator, ConstValidateCodeGenerator>()
	.AddHostedService<PaymentNotifyConsumer>()
	.AddHostedService<PushNotifyfConsumer>()
	.AddNotificationClient();

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
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetValue<string>("NatsSetting:Urls")
		}))
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
		.AddHandler(
			ProcessorTypes.BrandFrontendInfoChangedEventHandlerType,
			"tgs.brand.frontendinfo.changed")
		.AddHandler(
			ProcessorTypes.DepositFormStatusChangedHandlerType,
			"tgs.payment.deposit.status_ex")
		.AddHandler(
			ProcessorTypes.WithdrawFormStatusChangedEventHandlerType,
			"tgs.payment.withdraw.status_ex")
		.AddHandler(
			ProcessorTypes.NotificationHandlerType,
			"tgs.notify.broadcast"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddHttpLogging(logging => logging.LoggingFields = HttpLoggingFields.All)
	.AddHealthChecks();

var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
builder.Services
	.AddOpenTelemetry()
	.WithTracing(builder => builder
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/health")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !excludeExtensions.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterBuiler => meterBuiler
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
	.Services
	.AddProfileViewer()
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

builder.Services
	.AddSwaggerGen(c =>
	{
		c.SwaggerDoc(
			"v1",
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
		var corsDomain = builder.Configuration.GetSection("CorsDomain").Get<CorsDomain>()!;

		options.AddDefaultPolicy(policyBuilder => policyBuilder
			.WithOrigins(corsDomain.Urls)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.SetIsOriginAllowed(_ => true));
	});

var app = builder.Build();

app.UseHttpLogging();
app.UseForwardedHeaders();
app.UseRequestLocalization(options =>
{
	var config = app.Services.GetRequiredService<IConfiguration>();

	options.SupportedCultures.Clear();
	options.SupportedUICultures.Clear();

	config.GetSection("RequestLocalization").Bind(options);

	_ = options.SetDefaultCulture(config.GetValue<string>("RequestLocalization:DefaultRequestCulture")!);
});

app.UseBrandCertificate();
app.UseAuthentication();
app.UseRouting();
app.UseCors();
app.UseVerifyJwtTokenMiddlewares(Consts.DefaultBypassRouting);

app.MapControllers();
app.MapHub<NotifyHub>("api/notify");

app.MapHealthChecks("/health");
app.MapProfileViewer();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TGS Operator API"));

app.Run();
