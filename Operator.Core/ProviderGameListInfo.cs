using System.Collections.Generic;

namespace TGS.Services.Operator;
public class ProviderGameListInfo
{
	public string ProviderCode { get; set; }

	public int Total { get; set; }

	public IEnumerable<GameInfo> Games { get; set; }

}
