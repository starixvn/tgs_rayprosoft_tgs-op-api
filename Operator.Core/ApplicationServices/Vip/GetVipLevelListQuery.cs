using System;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator.ApplicationServices.Vip;

public record GetVipLevelListQuery(Guid BrandId, PageableRequest Paging);
