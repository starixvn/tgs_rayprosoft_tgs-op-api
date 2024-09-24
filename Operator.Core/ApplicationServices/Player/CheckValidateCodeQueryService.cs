using System;
using System.Threading.Tasks;
using TGS.Services.Operator.ValidateCode;
using Tgs.ServiceUtility;
using System.Threading;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckValidateCodeQueryService(IValidateCodeOperator validateCodeOperator) : IGetService<CheckValidateCodeQuery, bool>
{
	private readonly IValidateCodeOperator _validateCodeOperator =
			validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

	public async ValueTask<bool> GetAsync(CheckValidateCodeQuery query, CancellationToken cancellationToken = default)
	{
		return await _validateCodeOperator
			.CheckValidateCodeDataExistedAsync(query.ValidateCode, query.SecurityKey);
	}
}