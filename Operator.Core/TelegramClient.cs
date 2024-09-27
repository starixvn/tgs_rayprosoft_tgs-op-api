using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace TGS.Services.Operator;
public interface ITelegramClient
{
	Task SendMessageAsync(string message);
}

public class TelegramClient : ITelegramClient
{
	private readonly HttpClient _httpClient;
	private readonly TelegramServiceOptions _options;

	public TelegramClient(HttpClient httpClient, IOptions<TelegramServiceOptions> options)
	{
		_httpClient = httpClient;
		_options = options.Value ?? throw new ArgumentNullException(nameof(options));
	}

	public async Task SendMessageAsync(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
			throw new ArgumentException("Message cannot be null or empty", nameof(message));

		var payload = new FormUrlEncodedContent(new[]
		{
		 new KeyValuePair<string, string>("chat_id", _options.ChatId),
		 new KeyValuePair<string, string>("text", message)
	 });

		HttpResponseMessage response = await _httpClient.PostAsync("sendMessage", payload);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception("Failed to send message.");
		}
	}
}

public class TelegramServiceOptions
{
	public string BotToken { get; set; }
	public string ChatId { get; set; }
}

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddTelegramClient(this IServiceCollection services, Action<TelegramServiceOptions, IServiceProvider> configure)
	{
		return services.AddOptions<TelegramServiceOptions>()
			.Configure(configure)
			.Services
			.AddHttpClient<ITelegramClient, TelegramClient>()
			.ConfigureHttpClient((sp, httpClient) =>
			{
				// 從服務提供者中獲取已配置的 TelegramServiceOptions
				var options = sp.GetRequiredService<IOptions<TelegramServiceOptions>>().Value;

				// 使用選項配置 HttpClient
				httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{options.BotToken}/");
			})
			.Services;
	}
}