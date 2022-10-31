using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TGS.IdentityVerifiers;

public class ChineseIdentity15Verifier : IIdentityVerifier
{
	public bool Verify(string id)
	{
		var regex = new Regex(@"\d{15}");

		if (!regex.IsMatch(id))
			return false;

		var birthday = string.Concat("19", id.AsSpan(6, 6));

		return DateTime.TryParseExact(
			birthday,
			"yyyyMMdd",
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out _);
	}
}
