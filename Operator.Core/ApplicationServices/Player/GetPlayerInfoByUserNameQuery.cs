using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record GetPlayerInfoByUserNameQuery(Guid BrandId, string UserName);