using NUnit.Framework;
using TGS.IdentityVerifiers;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class ChineseIdentityTests
{
	[Test]
	public void Id_Length_15_Verify_Should_Correct()
	{
		var sut = new ChineseIdentityVerifier();

		var id = "320101000101105";

		var actual = sut.Verify(id);

		Assert.IsTrue(actual);
	}

	[Test]
	public void Id_Length_18_Verify_Should_Correct()
	{
		var sut = new ChineseIdentityVerifier();

		var id = "22010119000101191X";

		var actual = sut.Verify(id);

		Assert.IsTrue(actual);
	}
}
