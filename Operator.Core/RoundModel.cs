using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record RoundModel
{
    public Guid Id { get; init; }

    public string? ExternalRoundId { get; init; }

    public int GameId { get; init; }

    public string? GameName { get; init; }

    public Guid GameProviderId { get; init; }

    public string? GameProviderCode { get; init; }

    public string? GameProviderName { get; init; }

    public string? SubGameProviderName { get; init; }

    public string? PlayerName { get; init; }

    public string? PlayerCode { get; init; }

    public string? ExternalPlayerCode { get; init; }

    public string? CurrencyCode { get; init; }

    public PlatformType PlatformType { get; init; }

    public ClientRoundStatus Status { get; init; }

    public DateTimeOffset CreatedOn { get; init; }

    public DateTimeOffset? ClosedOn { get; init; }

    public DateTimeOffset UpdatedOn { get; init; }

    public DateTimeOffset? ExternalCreatedOn { get; init; }

    public DateTimeOffset? ExternalClosedOn { get; init; }

    public decimal? BetAmount { get; init; }

    public decimal? ValidBetAmount { get; init; }

    public decimal? Turnover { get; init; }

    public decimal? SettleAmount { get; init; }

    public decimal? Commission { get; init; }

    public decimal? WinLoss { get; init; }

    public decimal? TieBetAdjustment { get; init; }

    public string? AdditionalData { get; init; }
}