using System;
using System.Security.Claims;

namespace TGS.Services.Operator.ApplicationServices.Auth;

public record MakeBrandAuthTokenCommand(Guid BrandId, ClaimsIdentity Identity, string Site);
