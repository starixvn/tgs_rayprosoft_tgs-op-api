using System;
using Tgs.BrandCertificate;

#nullable enable

namespace TGS.Services.Operator;

public record BrandAgentDividendInfo(
	Guid BrandId,
	SettlementIntervalType SettlementIntervalType);