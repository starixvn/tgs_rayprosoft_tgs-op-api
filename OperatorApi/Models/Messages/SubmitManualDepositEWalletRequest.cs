using System;
using System.Text.Json.Serialization;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SubmitManualDepositEWalletRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("paymentid")]
    public Guid BrandPaymentId { get; init; }

    [JsonPropertyName("promotionid")]
    public Guid? PromotionId { get; init; }

    [JsonPropertyName("banktype")]
    public string BankType { get; init; } = default!;

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [JsonPropertyName("emoneyname")]
    public string? EMoneyName { get; init; }
}
