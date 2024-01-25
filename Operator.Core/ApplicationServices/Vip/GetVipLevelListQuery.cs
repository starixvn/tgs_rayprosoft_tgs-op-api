using System;
using Tgs.Paging;

namespace TGS.Services.Operator.ApplicationServices.Vip;

public record GetVipLevelListQuery(Guid BrandId, Paging<VipRecord> Paging);
