using System;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

public record SetHaveReadCommand(string PlayerCode, Guid LetterId);