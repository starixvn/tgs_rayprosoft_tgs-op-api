using Castle.Core.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace TGS.Apis.OperatorApi.Utils;

public static class IPUtils
{
	public static string GetRequestIP(HttpContext httpContext, bool tryUseXForwardHeader = true)
	{
		string ip = null;

		// todo support new "Forwarded" header (2014) https://en.wikipedia.org/wiki/X-Forwarded-For

		// X-Forwarded-For (csv list):  Using the First entry in the list seems to work
		// for 99% of cases however it has been suggested that a better (although tedious)
		// approach might be to read each IP from right to left and use the first public IP.
		// http://stackoverflow.com/a/43554000/538763
		//
		if (tryUseXForwardHeader)
			ip = SplitCsv(GetHeaderValueAs<string>(httpContext, "X-Forwarded-For")).FirstOrDefault();

		// RemoteIpAddress is always null in DNX RC1 Update1 (bug).
		if (ip.IsNullOrEmpty() && httpContext?.Connection?.RemoteIpAddress != null)
			ip = httpContext.Connection.RemoteIpAddress.ToString();

		if (ip.IsNullOrEmpty())
			ip = GetHeaderValueAs<string>(httpContext, "REMOTE_ADDR");

		// _httpContextAccessor.HttpContext?.Request?.Host this is the local host.

		if (ip.IsNullOrEmpty())
			throw new Exception("Unable to determine caller's IP.");

		return ip;
	}

	public static T GetHeaderValueAs<T>(HttpContext httpContext, string headerName)
	{
		StringValues values;

		if (httpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
		{
			string rawValues = values.ToString();   // writes out as Csv when there are multiple.

			if (!rawValues.IsNullOrEmpty())
				return (T)Convert.ChangeType(values.ToString(), typeof(T));
		}
		return default(T);
	}

	public static List<string> SplitCsv(string csvList, bool nullOrWhitespaceInputReturnsNull = false)
	{
		if (string.IsNullOrWhiteSpace(csvList))
			return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

		return csvList
			.TrimEnd(',')
			.Split(',')
			.AsEnumerable<string>()
			.Select(s => s.Trim())
			.ToList();
	}

	public static bool IsNullOrWhitespace(this string s)
	{
		return String.IsNullOrWhiteSpace(s);
	}
}
