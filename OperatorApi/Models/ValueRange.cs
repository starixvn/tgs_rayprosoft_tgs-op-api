namespace TGS.Apis.OperatorApi.Models;

public readonly struct ValueRange<T> where T : IComparable<T>
{
	public T From { get; init; }

	public T To { get; init; }

	public void Deconstruct(out T from, out T to)
	{
		from = From;
		to = To;
	}
}