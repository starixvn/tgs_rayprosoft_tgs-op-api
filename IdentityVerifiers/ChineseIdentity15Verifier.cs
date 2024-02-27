using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TGS.IdentityVerifiers;

public partial class ChineseIdentity15Verifier : IIdentityVerifier
{
	public bool Verify(string id)
	{
		var regex = CreateChineseIdentity15Regex();

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

	[GeneratedRegex("\\d{15}")]
	private static partial Regex CreateChineseIdentity15Regex();
}