namespace TGS.Apis.OperatorApi.Models.Messages;
#nullable enable

public class S88PayInfoViewModel
{
	public string MatchCode { get; set; } = default!;

	public string Qr { get; set; } = default!;

	public string Account { get; set; } = default!;

	public string AccountName { get; set; } = default!;

	public string? BankName { get; set; }

	public string Type { get; set; } = default!;
}