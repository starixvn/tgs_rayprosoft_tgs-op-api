using System;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;
public record SetHaveReadV2Command(string PlayerCode, Guid LetterId);