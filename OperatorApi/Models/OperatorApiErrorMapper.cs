using Tgs.Payment.Clients;
using Tgs.Payment.WandaPay.Client;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Interfaces;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.BrandPayment;
using TGS.Services.Operator.ValidateCode;
using PlayerNotFoundException = TGS.Core.Models.Exceptions.PlayerNotFoundException;

namespace TGS.Apis.OperatorApi.Models;

public class OperatorApiErrorMapper : IErrorMapper
{
	private static readonly IReadOnlyDictionary<OpApiError, string> _ErrorCodeMessageIdMap =
		new Dictionary<OpApiError, string>
		{
			[OpApiError.InvalidCredentials] = "InvalidCredentials",
			[OpApiError.InvalidOrExpiredPlayerToken] = "InvalidOrExpiredPlayerToken",
			[OpApiError.InvalidCaptcha] = "InvalidCaptcha",
			[OpApiError.ClientIdNotFound] = "ClientIdNotFound",
			[OpApiError.PlayerNotFound] = "PlayerNotFound",
			[OpApiError.UserBlocked] = "UserBlocked",
			[OpApiError.CountryRestricted] = "CountryRestricted",
			[OpApiError.CurrencyMismatch] = "CurrencyMismatch",
			[OpApiError.InsufficientFunds] = "InsufficientFunds",
			[OpApiError.InvalidInvitationCode] = "InvalidInvitationCode",
			[OpApiError.InvalidGameAction] = "InvalidGameAction",
			[OpApiError.TransactionIdNotFound] = "TransactionIdNotFound",
			[OpApiError.DuplicateTransaction] = "DuplicateTransaction",
			[OpApiError.InvalidAmount] = "InvalidAmount",
			[OpApiError.LoginFail] = "LoginFail",
			[OpApiError.InvalidArguments] = "InvalidArguments",
			[OpApiError.IncorrectFormat] = "IncorrectFormat",
			[OpApiError.UnderMaintenance] = "UnderMaintenance",
			[OpApiError.InvalidStatus] = "InvalidStatus",
			[OpApiError.SystemError] = "SystemError",
			[OpApiError.OperationNotAllowed] = "OperationNotAllowed",
			[OpApiError.ProviderApiError] = "ProviderApiError",
			[OpApiError.ValidateCodeNotVerify] = "ValidateCodeNotVerify",
			[OpApiError.EmailNotVerify] = "EmailNotVerify",
			[OpApiError.InsufficientBucket] = "InsufficientBucket",
			[OpApiError.PromotionCannotJoin] = "PromotionCannotJoin",
			[OpApiError.GameBalanceFundInFailed] = "GameBalanceFundInFailed",
			[OpApiError.GameBalanceFundOutFailed] = "GameBalanceFundOutFailed",
			[OpApiError.FavoriteFull] = "FavoriteAlreadyFull",
			[OpApiError.UserBanned] = "UserBanned",
			[OpApiError.BrandEmailDuplicate] = "BrandEmailDuplicate",
			[OpApiError.MaticPayDepositNeedRealName] = "MaticPayDepositNeedRealName",
			[OpApiError.PhoneNumberRequired] = "PhoneNumberRequired",
			[OpApiError.PlayerCanNotWithdraw] = "PlayerCanNotWithdraw",
			[OpApiError.BrandPhoneDuplicate] = "BrandPhoneDuplicate",
			[OpApiError.ZeroNineNinePayDepositNeedRealName] = "ZeroNineNinePayDepositNeedRealName",
			[OpApiError.PreviousDepositStillProcessing] = "PreviousDepositStillProcessing",
			[OpApiError.BankInfoDuplicate] = "BankInfoDuplicate",
			[OpApiError.WandaPayDepositNeedRealName] = "WandaPayDepositNeedChineseRealName",
			[OpApiError.WithdrawLimitExceed] = "WithdrawLimitExceed"
		};

	private static readonly IReadOnlyDictionary<OpApiError, int> _HttpCodeByException =
		new Dictionary<OpApiError, int>
		{
			[OpApiError.InvalidCredentials] = StatusCodes.Status401Unauthorized,
			[OpApiError.InvalidOrExpiredPlayerToken] = StatusCodes.Status401Unauthorized,
			[OpApiError.InvalidCaptcha] = StatusCodes.Status401Unauthorized,
			[OpApiError.ClientIdNotFound] = StatusCodes.Status401Unauthorized,
			[OpApiError.UserBlocked] = StatusCodes.Status403Forbidden,
			[OpApiError.CountryRestricted] = StatusCodes.Status403Forbidden,
			[OpApiError.CurrencyMismatch] = StatusCodes.Status500InternalServerError,
			[OpApiError.InsufficientFunds] = StatusCodes.Status500InternalServerError,
			[OpApiError.InvalidInvitationCode] = StatusCodes.Status500InternalServerError,
			[OpApiError.InvalidGameAction] = StatusCodes.Status500InternalServerError,
			[OpApiError.TransactionIdNotFound] = StatusCodes.Status500InternalServerError,
			[OpApiError.DuplicateTransaction] = StatusCodes.Status500InternalServerError,
			[OpApiError.InvalidAmount] = StatusCodes.Status500InternalServerError,
			[OpApiError.InvalidArguments] = StatusCodes.Status400BadRequest,
			[OpApiError.IncorrectFormat] = StatusCodes.Status400BadRequest,
			[OpApiError.UnderMaintenance] = StatusCodes.Status503ServiceUnavailable,
			[OpApiError.InvalidStatus] = StatusCodes.Status500InternalServerError,
			[OpApiError.LoginFail] = StatusCodes.Status400BadRequest,
			[OpApiError.SystemError] = StatusCodes.Status500InternalServerError,
			[OpApiError.OperationNotAllowed] = StatusCodes.Status405MethodNotAllowed,
			[OpApiError.ProviderApiError] = StatusCodes.Status500InternalServerError,
			[OpApiError.ValidateCodeNotVerify] = StatusCodes.Status406NotAcceptable,
			[OpApiError.PlayerNotFound] = StatusCodes.Status400BadRequest,
			[OpApiError.EmailNotVerify] = StatusCodes.Status400BadRequest,
			[OpApiError.InsufficientBucket] = StatusCodes.Status400BadRequest,
			[OpApiError.PromotionCannotJoin] = StatusCodes.Status400BadRequest,
			[OpApiError.GameBalanceFundInFailed] = StatusCodes.Status400BadRequest,
			[OpApiError.GameBalanceFundOutFailed] = StatusCodes.Status400BadRequest,
			[OpApiError.FavoriteFull] = StatusCodes.Status400BadRequest,
			[OpApiError.UserBanned] = StatusCodes.Status403Forbidden,
			[OpApiError.BrandEmailDuplicate] = StatusCodes.Status500InternalServerError,
			[OpApiError.MaticPayDepositNeedRealName] = StatusCodes.Status400BadRequest,
			[OpApiError.PhoneNumberRequired] = StatusCodes.Status400BadRequest,
			[OpApiError.PlayerCanNotWithdraw] = StatusCodes.Status400BadRequest,
			[OpApiError.BrandPhoneDuplicate] = StatusCodes.Status400BadRequest,
			[OpApiError.ZeroNineNinePayDepositNeedRealName] = StatusCodes.Status400BadRequest,
			[OpApiError.PreviousDepositStillProcessing] = StatusCodes.Status400BadRequest,
			[OpApiError.BankInfoDuplicate] = StatusCodes.Status409Conflict,
			[OpApiError.WandaPayDepositNeedRealName] = StatusCodes.Status400BadRequest,
			[OpApiError.WithdrawLimitExceed] = StatusCodes.Status400BadRequest
		};

	private static readonly IReadOnlyDictionary<Type, OpApiError> _OpApiErrorByException =
		new Dictionary<Type, OpApiError>
		{
			[typeof(InvalidCredentialsException)] = OpApiError.InvalidCredentials,
			[typeof(MissingTokenException)] = OpApiError.InvalidOrExpiredPlayerToken,
			[typeof(PlayerLoginExpiredException)] = OpApiError.InvalidOrExpiredPlayerToken,
			[typeof(InvalidCaptchaException)] = OpApiError.InvalidCaptcha,
			[typeof(ClientIdNotFoundException)] = OpApiError.ClientIdNotFound,
			[typeof(PlayerNotFoundException)] = OpApiError.PlayerNotFound,
			[typeof(PlayerDisabledException)] = OpApiError.UserBlocked,
			[typeof(CountryRestrictedException)] = OpApiError.CountryRestricted,
			[typeof(CurrencyNotMatchingException)] = OpApiError.CurrencyMismatch,
			[typeof(PlayerHasInsufficientFundsException)] = OpApiError.InsufficientFunds,
			[typeof(InvalidInvitationCodeException)] = OpApiError.InvalidInvitationCode,
			[typeof(InvalidGameActionException)] = OpApiError.InvalidGameAction,
			[typeof(InvalidAmountException)] = OpApiError.InvalidAmount,
			[typeof(ArgumentException)] = OpApiError.InvalidArguments,
			[typeof(ModelBindingException)] = OpApiError.InvalidArguments,
			[typeof(FormatException)] = OpApiError.IncorrectFormat,
			[typeof(BrandUnderMaintenanceException)] = OpApiError.UnderMaintenance,
			[typeof(InvalidStatusException)] = OpApiError.InvalidStatus,
			[typeof(InvalidOperationException)] = OpApiError.SystemError,
			[typeof(OperationNotAllowedException)] = OpApiError.OperationNotAllowed,
			[typeof(GameProviderApiErrorException)] = OpApiError.ProviderApiError,
			[typeof(Services.Operator.PlayerNotFoundException)] = OpApiError.PlayerNotFound,
			[typeof(ValidateCodeNotVerifyException)] = OpApiError.ValidateCodeNotVerify,
			[typeof(EmailNotVerifyException)] = OpApiError.EmailNotVerify,
			[typeof(InsufficientBucketException)] = OpApiError.InsufficientBucket,
			[typeof(CannotJoinPromotionException)] = OpApiError.PromotionCannotJoin,
			[typeof(GameBalanceFundInFailedException)] = OpApiError.GameBalanceFundInFailed,
			[typeof(GameBalanceFundOutFailedException)] = OpApiError.GameBalanceFundOutFailed,
			[typeof(LoginFailException)] = OpApiError.LoginFail,
			[typeof(FavoriteCountFullException)] = OpApiError.FavoriteFull,
			[typeof(UserBannedException)] = OpApiError.UserBanned,
			[typeof(BrandEmailDuplicateException)] = OpApiError.BrandEmailDuplicate,
			[typeof(MaticPayDepositNeedRealNameException)] = OpApiError.MaticPayDepositNeedRealName,
			[typeof(PlayerPhoneRequiredException)] = OpApiError.PhoneNumberRequired,
			[typeof(PlayerCanNotWithdrawException)] = OpApiError.PlayerCanNotWithdraw,
			[typeof(BrandPhoneDuplicateException)] = OpApiError.BrandPhoneDuplicate,
			[typeof(ZeroNineNinePayDepositNeedRealNameException)] = OpApiError.ZeroNineNinePayDepositNeedRealName,
			[typeof(PreviousDepositStillProcessingException)] = OpApiError.PreviousDepositStillProcessing,
			[typeof(PlayerBankInfoDuplicateException)] = OpApiError.BankInfoDuplicate,
			[typeof(WandaPayNeedChineseRealNameException)] = OpApiError.WandaPayDepositNeedRealName,
			[typeof(WithdrawLimitExceedException)] = OpApiError.WithdrawLimitExceed
		};

	public bool CanProcess(Exception exception)
		=> true;

	public int GetApiErrorByException(Exception exception)
	{
		if (exception == null)
		{
			return (int)OpApiError.None;
		}

		var type = exception.GetType();
		return _OpApiErrorByException.TryGetValue(type, out var error)
			? (int)error
			: (int)OpApiError.SystemError;
	}

	public int GetHttpStatusByException(Exception exception)
	{
		if (exception == null)
		{
			return StatusCodes.Status200OK;
		}

		var type = exception.GetType();
		return _OpApiErrorByException.TryGetValue(type, out var error) &&
			_HttpCodeByException.TryGetValue(error, out var statusCode)
				? statusCode
				: StatusCodes.Status500InternalServerError;
	}

	public string GetMessageByException(Exception exception)
		=> exception.Message;

	public string GetMessageKeyByException(Exception exception)
	{
		var type = exception.GetType();

		return _OpApiErrorByException.TryGetValue(type, out var errorType)
			&& _ErrorCodeMessageIdMap.TryGetValue(errorType, out var messageKey)
			? messageKey
			: _ErrorCodeMessageIdMap[OpApiError.SystemError];
	}
}
