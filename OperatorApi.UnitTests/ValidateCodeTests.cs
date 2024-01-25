using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public partial class ValidateCodeTests
{
	[Test]
	public void Validate_Code_Format_Should_Match()
	{
		var sut = new RandomValidateCodeGenerator();
		var regex = CreateValidateCodeRegex();

		var actual = sut.GenerateCode();

		ClassicAssert.IsTrue(regex.IsMatch(actual));
	}

	[GeneratedRegex("^\\d{6}$")]
	private static partial Regex CreateValidateCodeRegex();
}
