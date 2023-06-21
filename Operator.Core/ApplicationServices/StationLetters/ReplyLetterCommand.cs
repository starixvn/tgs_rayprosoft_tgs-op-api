using System;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;
public record ReplyLetterCommand(
	string PlayerCode,
	Guid LetterId,
	string Content,
	string Operator);
