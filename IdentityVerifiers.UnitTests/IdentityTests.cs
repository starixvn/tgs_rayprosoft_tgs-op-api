using NUnit.Framework;
using TGS.IdentityVerifiers;

namespace TGS.Tests.IdentityVerifiers.UnitTests;

[TestFixture]
public class IdentityTests
{
	[Test]
	public void Currency_Is_CNY_IdentityVerifier_Should_Use_ChineseIdentityVerifier()
	{
		var currency = "CNY";

		var sut = new CurrencyIdentityVerifierSelector();

		var actual = sut.GetIdentityVerifier(currency);

		Assert.IsInstanceOf<ChineseIdentityVerifier>(actual);
	}

	[Test]
	public void Currency_Is_THB_IdentityVerifier_Should_Use_SoutheastAsiaIdentityVerifier()
	{
		var currency = "THB";

		var sut = new CurrencyIdentityVerifierSelector();

		var actual = sut.GetIdentityVerifier(currency);

		Assert.IsInstanceOf<SoutheastAsiaIdentityVerifier>(actual);
	}
}
