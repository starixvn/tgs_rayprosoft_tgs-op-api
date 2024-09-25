using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using Castle.Core.Internal;
using TGS.Operator.Repositories.Models;

namespace TGS.Operator.Repositories.Extensions;
public static class GamesExtension
{
	public static IQueryable<IGrouping<object, GameDto>> GroupByGameQuery(this IQueryable<GameDto> source, params (IEnumerable, Expression<Func<GameDto, object>>)[] keySelectors)
	{
		foreach (var selector in keySelectors)
		{
			if (!selector.Item1.IsNullOrEmpty())
			{
				return source.GroupBy(selector.Item2);
			}
		}

		return source.GroupBy(s => (object)s.Type);
	}
}
