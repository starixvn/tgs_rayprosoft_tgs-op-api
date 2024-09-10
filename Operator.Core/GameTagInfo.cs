using System.Collections.Generic;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;
public class GameTagInfo
{
	public GameTag Tag { get; set; }

	public IEnumerable<GameInfo> games { get; set; }
}
