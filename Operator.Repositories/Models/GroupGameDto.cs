using System.Collections.Generic;

namespace TGS.Operator.Repositories.Models;
public class GroupGameDto
{
	public object Key { get; set; }

	public int Total { get; set; }

	public IEnumerable<GameDto> Games { get; set; }
}
