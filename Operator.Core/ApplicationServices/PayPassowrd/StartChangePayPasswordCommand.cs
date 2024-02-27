using System.Globalization;

namespace TGS.Services.Operator.ApplicationServices.PayPassowrd;

public record StartChangePayPasswordCommand(string PlayerCode, CultureInfo? CultureInfo);