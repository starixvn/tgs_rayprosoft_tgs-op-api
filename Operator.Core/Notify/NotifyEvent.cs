using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;

namespace TGS.Services.Operator.Notify;
public record NotifyEvent
{
	private readonly NameValueCollection _tags = new();

	public JsonDocument Body { get; private set; }

	public NotifyEvent(IEnumerable<KeyValuePair<string, string>> tags, string body)
	{
		foreach (var tag in tags)
			_tags.Add(tag.Key, tag.Value);

		Body = JsonDocument.Parse(body);
	}

	public string? this[string name] => GetTagValues(name)?[^1];

	public string[]? GetTagValues(string name) => _tags.GetValues(name);

	public bool Match(string name, string value)
		=> GetTagValues(name)
			?.Any(v => v.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? false;
}
