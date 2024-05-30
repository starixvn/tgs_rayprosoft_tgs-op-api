using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using HttpOverStream.NamedPipe;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using OpenTelemetry.Exporter;
using Tgs.BrandGameCredits.Common;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Infrastructures.Redis;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace OperatorApi.IntegrationTests;

internal static class TestOperatorApplication
{
	public static readonly RsaSecurityKey TestSecurityKey = new(RSA.Create());
	private static readonly JwtSecurityTokenHandler _JwtSecurityTokenHandler = new();

	public static IHostBuilder Builder => new FixToolApplication().Builder
		.ConfigureLogging((ctx, logging) =>
		{
			_ = logging.ClearProviders();
			_ = logging.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);
		})
		.ConfigureServices(services => services
			.AddFakeNatsMessageQueue()
			.AddSingleton(Substitute.For<IRedisConnectionFactory>())
			.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
				sp => new ConfigureNamedOptions<OtlpExporterOptions>(
					Options.DefaultName,
					exporterOptions => exporterOptions.TimeoutMilliseconds = 0))
			.AddSingleton(Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>())
			.Replace(ServiceDescriptor.Singleton(sp =>
			{
				var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
				_ = dbContextOptionsBuilder
					.UseInMemoryDatabase(Guid.NewGuid().ToString())
					.ConfigureWarnings(x => x.Ignore(
						InMemoryEventId.TransactionIgnoredWarning));

				return new TgsRepository(dbContextOptionsBuilder.Options);
			})));

	public static string MakeTestBrandToken(Guid brandId, string brandCode)
	{
		var id = new ClaimsIdentity([
			new Claim(JwtRegisteredClaimNames.Name, brandCode),
			new Claim(JwtRegisteredClaimNames.Sub, brandId.ToString())
		]);

		var token = _JwtSecurityTokenHandler.CreateToken(new SecurityTokenDescriptor
		{
			Issuer = "tgs",
			Subject = id,
			SigningCredentials = new SigningCredentials(TestSecurityKey, SecurityAlgorithms.RsaSha256)
		});

		return _JwtSecurityTokenHandler.WriteToken(token);
	}

	internal static IHostBuilder UseTestChannelServer(this IHostBuilder hostBuilder, string channelName)
	{
		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		return isWindows
			? hostBuilder
				.ConfigureWebHost(webHostBuilder => webHostBuilder
					.UseHttpOverStreamServer(new NamedPipeListener(channelName)))
			: hostBuilder
				.ConfigureWebHost(webHostBuilder => webHostBuilder
					.UseKestrel(options => options.ListenUnixSocket(
						 $"/tmp/{channelName}.sock")));
	}

	private class FixToolApplication : WebApplicationFactory<Program>
	{
		private IHostBuilder? _hostBuilder;

		public IHostBuilder Builder
		{
			get
			{
				_ = Server;
				return _hostBuilder!;
			}
		}

		protected override IHost CreateHost(IHostBuilder builder)
		{
			_hostBuilder = builder;

			return Host.CreateDefaultBuilder()
				.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer())
				.Build();
		}
	}
}