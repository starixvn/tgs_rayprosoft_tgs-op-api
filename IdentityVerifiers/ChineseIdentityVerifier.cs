using System.Linq;

namespace TGS.IdentityVerifiers;

public class ChineseIdentityVerifier : IIdentityVerifier
{
	public bool Verify(string id)
	{
		var cityCodes = new[]
		{
			"11", "12", "13", "14", "15", "21", "22", "23", "31", "32", "33", "34", "35", "36", "37", "41",
			"42", "43", "44", "45", "46", "50", "51", "52", "53", "54", "61", "62", "63", "64", "65", "71",
			"81", "82", "91"
		};

		var cityCode = id[..2];

		return cityCodes.Contains(cityCode) && GetVerifierByLength(id.Length).Verify(id);
	}

	private static IIdentityVerifier GetVerifierByLength(int length) => length switch
	{
		15 => new ChineseIdentity15Verifier(),
		18 => new ChineseIdentity18Verifier(),
		_ => new AlwaysFailVerifier()
	};
}
