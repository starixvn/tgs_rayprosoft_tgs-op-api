using System.Collections.Generic;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;
public class GameTagInfo
{
	public GameTag Tag { get; set; }

	public int Total { get; set; }

	public IEnumerable<GameInfo> Games { get; set; }
}
