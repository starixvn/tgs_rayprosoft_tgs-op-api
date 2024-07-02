namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetContributionsRequest : PagingRequest
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}