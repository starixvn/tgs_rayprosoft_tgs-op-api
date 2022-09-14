using System;
using System.Text.Json.Serialization;
using TGS.Services.Operator.ApplicationServices.GameBalance;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GameBalanceInfosViewModel
{
    public GameBalanceInfosViewModel()
    {
    }

    public GameBalanceInfosViewModel(GameBalanceRecord record)
    {
        this.From = record.From;
        this.To = record.To;
        this.Amount = record.Amount;
        this.BalanceTime = record.BalanceTime;
    }

    [JsonPropertyName("from")]
    public string From { get; init; } = default!;

    [JsonPropertyName("to")]
    public string To { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("balancetime")]
    public DateTimeOffset BalanceTime { get; init; }
}