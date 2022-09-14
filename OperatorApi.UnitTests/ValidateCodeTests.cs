using System.Text.RegularExpressions;
using NUnit.Framework;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class ValidateCodeTests
{
    [Test]
    public void Validate_Code_Format_Should_Match()
    {
        var sut = new RandomValidateCodeGenerator();
        var regex = new Regex("^\\d{6}$");

        var actual = sut.GenerateCode();

        Assert.IsTrue(regex.IsMatch(actual));
    }
}
