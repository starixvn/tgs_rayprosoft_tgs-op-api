using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TGS.Apis.OperatorApi;

public class Program
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, builder) =>
            {
                builder.AddJsonFile("/config/appsettings.json", true).AddEnvironmentVariables();
            })
            .ConfigureLogging((ctx, logging) =>
            {
                logging
                    .AddOpenTelemetry(options =>
                    {
                        options.IncludeFormattedMessage = true;
                        options.ParseStateValues = true;
                        _ = options.AddFluentdForwardExporter(fluentOptions =>
                        {
                            var nodeIp = ctx.Configuration.GetValue<string>("NODE_IP");
                            fluentOptions.Host = nodeIp;
                            fluentOptions.Tag = ctx.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
                            fluentOptions.UseMessagePack();
                        });
                    });
            })
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());

    public static async Task Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }
}
