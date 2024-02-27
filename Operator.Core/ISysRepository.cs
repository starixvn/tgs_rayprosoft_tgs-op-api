using System.Collections.Generic;

namespace TGS.Services.Operator;

public interface ISysRepository
{
	IAsyncEnumerable<KeyValuePair<string, string>> QueryAllLocalizationStringsAsync(string cultureCode);
}