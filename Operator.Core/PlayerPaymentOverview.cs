using System;

namespace TGS.Services.Operator;
public record PlayerPaymentOverview(
    string PlayerCode,
    decimal? FirstDepositAmount,
    decimal? FirstDepositHandlingFee,
    DateTime? FirstDepositOn,
    decimal? FirstWithdrawAmount,
    decimal? FirstWithdrawHandlingFee,
    DateTime? FirstWithdrawOn,
    DateOnly RecordedBy,
    decimal TotalDepositAmount,
    decimal TotalWithdrawAmount,
    int TotalDepositCount,
    int TotalWithdrawCount);
