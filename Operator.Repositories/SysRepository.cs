using System;
using System.Collections.Generic;
using System.Linq;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class SysRepository(ITgsRepository tgsRepository) : ISysRepository
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public IAsyncEnumerable<KeyValuePair<string, string>> QueryAllLocalizationStringsAsync(string cultureCode)
	{
		var rows = _tgsRepository.SystemLocalizations.AsQueryable()
			.Where(loc => loc.SystemCode == "OP-API" && loc.CultureCode == cultureCode)
			.Select(loc => new
			{
				loc.MessageId,
				loc.Text
			});

		return rows.ToAsyncEnumerable()
			.Select(item => new KeyValuePair<string, string>(item.MessageId, item.Text));
	}
}
