using System;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IS88PayRepository
{
	ValueTask<string?> GetDepositAsync(Guid id);

	ValueTask<bool> StoreDepositAsync(Guid id, string json);
}