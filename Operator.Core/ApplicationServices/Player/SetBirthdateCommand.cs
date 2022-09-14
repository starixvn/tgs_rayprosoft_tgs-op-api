using System;

namespace TGS.Services.Operator.ApplicationServices.Player;
public record SetBirthdateCommand(string PlayerCode, DateOnly? Birthdate);
