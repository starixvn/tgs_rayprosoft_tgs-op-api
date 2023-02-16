using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record PublishPlayerLoggedInEventCommand(
	string PlayerCode,
	DateTimeOffset LoggedInTime);