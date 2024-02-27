#nullable enable

using System.Collections.Generic;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;
public record SendStationLetterCommand(
	string PlayerCode,
	IEnumerable<string> Receivers,
	string Subject,
	string Content,
	string Operator);