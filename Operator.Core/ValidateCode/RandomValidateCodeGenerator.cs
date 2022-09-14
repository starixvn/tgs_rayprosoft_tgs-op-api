using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace TGS.Services.Operator.ValidateCode;

internal class RandomValidateCodeGenerator : IValidateCodeGenerator
{
    public string GenerateCode()
    {
        return string.Join(string.Empty, CreateNumberSerial().Take(6));
    }

    public string GenerateSecurityKey()
    {
        return Guid.NewGuid().ToString();
    }

    private IEnumerable<int> CreateNumberSerial()
    {
        while (true)
            yield return RandomNumberGenerator.GetInt32(10);
    }
}
