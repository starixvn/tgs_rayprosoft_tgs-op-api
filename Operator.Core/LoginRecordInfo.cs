using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record LoginRecordInfo(
	Guid Id,
	string PlayerCode,
	LoginStatus Status,
	PlatformType PlatformType,
	DateTimeOffset CreatedOn,
	DateTimeOffset ExpiredOn);