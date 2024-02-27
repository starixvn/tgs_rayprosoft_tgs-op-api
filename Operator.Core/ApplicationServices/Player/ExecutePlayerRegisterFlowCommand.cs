using System;
using System.Text.Json;

namespace TGS.Services.Operator.ApplicationServices.Player;
public record ExecutePlayerRegisterFlowCommand(
	Guid BrandId,
	string StepType,
	JsonDocument Input,
	PlayerLocationInfo LocationInfo);