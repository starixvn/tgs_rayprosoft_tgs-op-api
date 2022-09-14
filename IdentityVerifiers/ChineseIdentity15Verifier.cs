using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TGS.IdentityVerifiers;

public class ChineseIdentity15Verifier : IIdentityVerifier
{
    public bool Verify(string id)
    {
        var regex = new Regex(@"\d{15}");

        if (!regex.IsMatch(id))
            return false;

        var birthday = "19" + id.Substring(6, 6);
        var sequence = id.Substring(12, 3);

        if (!DateTime.TryParseExact(
            birthday,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _))
            return false;

        return true;
    }
}
