using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Utils;

namespace TGS.Apis.OperatorApi.Controllers;


[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class InfoController(IConfiguration configuration) :ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Index([FromServices] IWebHostEnvironment _hostingEnvironment)
	{
		var hostName = Dns.GetHostName();
		var hostEntry = await Dns.GetHostEntryAsync(hostName).ConfigureAwait(false);
		var hostIp = Array.Find(hostEntry.AddressList,
			x => x.AddressFamily == AddressFamily.InterNetwork);

		return Content(JsonSerializer.Serialize(new
		{
			Environment.MachineName,
			HostName = hostName,
			HostIp = hostIp?.ToString() ?? string.Empty,
			Environment = _hostingEnvironment.EnvironmentName,
			OsVersion = $"{Environment.OSVersion}",
			Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
			ProcessCount = Environment.ProcessorCount,
			ReleaseNamespace = configuration.GetValue<string>("RELEASE_NAMESPACE"),
			PodIPAddress = configuration.GetValue<string>("POD_IP"),
			PlayerIp = IPUtils.GetRequestIP(HttpContext)
		}));
	}
}
