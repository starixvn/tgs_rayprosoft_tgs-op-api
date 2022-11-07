using System;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IHelp2PayRepository
{
	ValueTask<string?> GetDepositHtmlAsync(Guid identityId);

	ValueTask<bool> StoreDepositHtmlAsync(Guid identityId, string html);
}
