using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TGS.IdentityVerifiers;

public partial class ChineseIdentity18Verifier : IIdentityVerifier
{
	public bool Verify(string id)
	{
		var regex = CreateChineseIdentity17Regex();

		if (!regex.IsMatch(id))
			return false;

		var weights = new[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };

		var areaCode = id[..6];
		var birthday = id.Substring(6, 8);
		var sequence = id.Substring(12, 3);
		var checkCode = id.Last();

		if (!DateTime.TryParseExact(
			birthday,
			"yyyyMMdd",
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out _))
			return false;

		var sum = id[0..^1].Zip(weights).Select(t => (t.First - '0') * t.Second).Sum();
		var proof = (12 - sum % 11) % 11;

		return proof < 10 ? proof == checkCode - '0' : checkCode == 'X';
	}

	[GeneratedRegex("\\d{17}\\d|X")]
	private static partial Regex CreateChineseIdentity17Regex();
}