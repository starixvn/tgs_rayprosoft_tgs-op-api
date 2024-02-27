using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class TransactionLogQueryRequest
{
	[FromQuery(Name = "from")]
	public DateTime From { get; set; }

	[FromQuery(Name = "page")]
	public int? Page { get; set; }

	[FromQuery(Name = "rows")]
	public int? Rows { get; set; }

	[FromQuery(Name = "to")]
	public DateTime To { get; set; }
}