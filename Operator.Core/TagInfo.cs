namespace TGS.Services.Operator;

public record TagInfo
{
	public bool New { get; init; }

	public bool Hot { get; init; }

	public bool Recommend { get; init; }
}