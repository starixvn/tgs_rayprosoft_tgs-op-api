using System;
using System.Threading.Tasks;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator;

public interface IGameProviderStatusValidator
{
	ValueTask CheckGameProviderStatusAsync(PlayerTokenData playerTokenData, string gameProviderCode, Guid brandId);
}