using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GameProviderStatusModel
{
	public string GpCode { get; set; }

	public Status Status { get; set; }
}