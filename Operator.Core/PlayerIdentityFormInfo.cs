using System;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Services.Operator;

public record PlayerIdentityFormInfo(Guid? FormId, PlayerIdentityReviewState Status);
