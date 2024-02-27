using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record GetLogoutQuery(Guid BrandId, string PlayerCode, Guid LoginRecordId);