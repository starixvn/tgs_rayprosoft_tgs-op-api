using NUnit.Framework;
using NUnit.Framework.Legacy;
using TGS.IdentityVerifiers;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class SoutheastAsiaIdentityTests
{
	[Test]
	public void SoutheastAsia_Identity_Always_Correct()
	{
		var sut = new SoutheastAsiaIdentityVerifier();

		var id = "anywords";

		var actual = sut.Verify(id);

		ClassicAssert.IsTrue(actual);
	}
}