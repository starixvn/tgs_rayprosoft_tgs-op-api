using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TGS.Services.Operator.StationLetters;

public record StationLetter(
	Guid Id,
	string Subject,
	string Content,
	DateTimeOffset CreateOn,
	bool IsRead,
	IDictionary<string, JsonDocument>? Localization);
