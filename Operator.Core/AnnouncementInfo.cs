using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TGS.Services.Operator;

public record AnnouncementInfo(
	string Subject,
	string Content,
	DateTimeOffset From,
	DateTimeOffset To,
	int DisplayOrder,
	IDictionary<string, JsonDocument>? Localization,
	string Type,
	DateTimeOffset CreatedOn);
