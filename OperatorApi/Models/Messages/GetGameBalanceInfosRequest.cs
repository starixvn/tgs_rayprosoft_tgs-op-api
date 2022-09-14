using System;
using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetGameBalanceInfosRequest
{
    [FromQuery(Name = "beginrecord")]
    public DateTimeOffset? BeginRecordOn { get; init; }

    [FromQuery(Name = "endrecord")]
    public DateTimeOffset? EndRecordOn { get; init; }
}