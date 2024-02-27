using System;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IPlayerOnlineRepository
{
	Task SetAsync(Guid brandId, string playerCode);
}