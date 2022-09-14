using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Enums;
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
    private static readonly IReadOnlyDictionary<TgsApiError, string> _ErrorCodeMessageIdMap =
        new Dictionary<TgsApiError, string>
        {
            [TgsApiError.InvalidCredentials] = "InvalidCredentials",
            [TgsApiError.InvalidOrExpiredPlayerToken] = "InvalidOrExpiredPlayerToken",
            [TgsApiError.InvalidCaptcha] = "InvalidCaptcha",
            [TgsApiError.ClientIdNotFound] = "ClientIdNotFound",
            [TgsApiError.PlayerNotFound] = "PlayerNotFound",
            [TgsApiError.UserBlocked] = "UserBlocked",
            [TgsApiError.CountryRestricted] = "CountryRestricted",
            [TgsApiError.CurrencyMismatch] = "CurrencyMismatch",
            [TgsApiError.InsufficientFunds] = "InsufficientFunds",
            [TgsApiError.InvalidInvitationCode] = "InvalidInvitationCode",
            [TgsApiError.InvalidGameAction] = "InvalidGameAction",
            [TgsApiError.TransactionIdNotFound] = "TransactionIdNotFound",
            [TgsApiError.DuplicateTransaction] = "DuplicateTransaction",
            [TgsApiError.InvalidAmount] = "InvalidAmount",
            [TgsApiError.LoginFail] = "LoginFail",
            [TgsApiError.InvalidArguments] = "InvalidArguments",
            [TgsApiError.IncorrectFormat] = "IncorrectFormat",
            [TgsApiError.UnderMaintenance] = "UnderMaintenance",
            [TgsApiError.InvalidStatus] = "InvalidStatus",
            [TgsApiError.SystemError] = "SystemError",
            [TgsApiError.OperationNotAllowed] = "OperationNotAllowed",
            [TgsApiError.ProviderApiError] = "ProviderApiError",
            [TgsApiError.ValidateCodeNotVerify] = "ValidateCodeNotVerify",
            [TgsApiError.EmailNotVerify] = "EmailNotVerify",
            [TgsApiError.InsufficientBucket] = "InsufficientBucket",
            [TgsApiError.PromotionCannotJoin] = "PromotionCannotJoin",
            [TgsApiError.GameBalanceFundInFailed] = "GameBalanceFundInFailed",
            [TgsApiError.GameBalanceFundOutFailed] = "GameBalanceFundOutFailed",
            [TgsApiError.FavoriteFull] = "FavoriteAlreadyFull",
            [TgsApiError.UserBanned] = "UserBanned",
            [TgsApiError.BrandEmailDuplicate] = "BrandEmailDuplicate",
            [TgsApiError.MaticPayDepositNeedRealName] = "MaticPayDepositNeedRealName",
            [TgsApiError.PhoneNumberRequired] = "PhoneNumberRequired",
            [TgsApiError.PlayerCanNotWithdraw] = "PlayerCanNotWithdraw",
            [TgsApiError.BrandPhoneDuplicate] = "BrandPhoneDuplicate",
            [TgsApiError.ZeroNineNinePayDepositNeedRealName] = "ZeroNineNinePayDepositNeedRealName",
            [TgsApiError.PreviousDepositStillProcessing] = "PreviousDepositStillProcessing"
        };

    private static readonly IReadOnlyDictionary<TgsApiError, int> _HttpCodeByException =
        new Dictionary<TgsApiError, int>
        {
            [TgsApiError.InvalidCredentials] = StatusCodes.Status401Unauthorized,
            [TgsApiError.InvalidOrExpiredPlayerToken] = StatusCodes.Status401Unauthorized,
            [TgsApiError.InvalidCaptcha] = StatusCodes.Status401Unauthorized,
            [TgsApiError.ClientIdNotFound] = StatusCodes.Status401Unauthorized,
            [TgsApiError.PlayerNotFound] = StatusCodes.Status401Unauthorized,
            [TgsApiError.UserBlocked] = StatusCodes.Status403Forbidden,
            [TgsApiError.CountryRestricted] = StatusCodes.Status403Forbidden,
            [TgsApiError.CurrencyMismatch] = StatusCodes.Status500InternalServerError,
            [TgsApiError.InsufficientFunds] = StatusCodes.Status500InternalServerError,
            [TgsApiError.InvalidInvitationCode] = StatusCodes.Status500InternalServerError,
            [TgsApiError.InvalidGameAction] = StatusCodes.Status500InternalServerError,
            [TgsApiError.TransactionIdNotFound] = StatusCodes.Status500InternalServerError,
            [TgsApiError.DuplicateTransaction] = StatusCodes.Status500InternalServerError,
            [TgsApiError.InvalidAmount] = StatusCodes.Status500InternalServerError,
            [TgsApiError.InvalidArguments] = StatusCodes.Status400BadRequest,
            [TgsApiError.IncorrectFormat] = StatusCodes.Status400BadRequest,
            [TgsApiError.UnderMaintenance] = StatusCodes.Status503ServiceUnavailable,
            [TgsApiError.InvalidStatus] = StatusCodes.Status500InternalServerError,
            [TgsApiError.LoginFail] = StatusCodes.Status400BadRequest,
            [TgsApiError.SystemError] = StatusCodes.Status500InternalServerError,
            [TgsApiError.OperationNotAllowed] = StatusCodes.Status405MethodNotAllowed,
            [TgsApiError.ProviderApiError] = StatusCodes.Status500InternalServerError,
            [TgsApiError.ValidateCodeNotVerify] = StatusCodes.Status406NotAcceptable,
            [TgsApiError.PlayerNotFound] = StatusCodes.Status400BadRequest,
            [TgsApiError.EmailNotVerify] = StatusCodes.Status400BadRequest,
            [TgsApiError.InsufficientBucket] = StatusCodes.Status400BadRequest,
            [TgsApiError.PromotionCannotJoin] = StatusCodes.Status400BadRequest,
            [TgsApiError.GameBalanceFundInFailed] = StatusCodes.Status400BadRequest,
            [TgsApiError.GameBalanceFundOutFailed] = StatusCodes.Status400BadRequest,
            [TgsApiError.FavoriteFull] = StatusCodes.Status400BadRequest,
            [TgsApiError.UserBanned] = StatusCodes.Status403Forbidden,
            [TgsApiError.BrandEmailDuplicate] = StatusCodes.Status500InternalServerError,
            [TgsApiError.MaticPayDepositNeedRealName] = StatusCodes.Status400BadRequest,
            [TgsApiError.PhoneNumberRequired] = StatusCodes.Status400BadRequest,
            [TgsApiError.PlayerCanNotWithdraw] = StatusCodes.Status400BadRequest,
            [TgsApiError.BrandPhoneDuplicate] = StatusCodes.Status400BadRequest,
            [TgsApiError.ZeroNineNinePayDepositNeedRealName] = StatusCodes.Status400BadRequest,
            [TgsApiError.PreviousDepositStillProcessing] = StatusCodes.Status400BadRequest
        };

    private static readonly IReadOnlyDictionary<Type, TgsApiError> _TgsApiErrorByException =
        new Dictionary<Type, TgsApiError>
        {
            [typeof(InvalidCredentialsException)] = TgsApiError.InvalidCredentials,
            [typeof(MissingTokenException)] = TgsApiError.InvalidOrExpiredPlayerToken,
            [typeof(PlayerLoginExpiredException)] = TgsApiError.InvalidOrExpiredPlayerToken,
            [typeof(InvalidCaptchaException)] = TgsApiError.InvalidCaptcha,
            [typeof(ClientIdNotFoundException)] = TgsApiError.ClientIdNotFound,
            [typeof(PlayerNotFoundException)] = TgsApiError.PlayerNotFound,
            [typeof(PlayerDisabledException)] = TgsApiError.UserBlocked,
            [typeof(CountryRestrictedException)] = TgsApiError.CountryRestricted,
            [typeof(CurrencyNotMatchingException)] = TgsApiError.CurrencyMismatch,
            [typeof(PlayerHasInsufficientFundsException)] = TgsApiError.InsufficientFunds,
            [typeof(InvalidInvitationCodeException)] = TgsApiError.InvalidInvitationCode,
            [typeof(InvalidGameActionException)] = TgsApiError.InvalidGameAction,
            [typeof(InvalidAmountException)] = TgsApiError.InvalidAmount,
            [typeof(ArgumentException)] = TgsApiError.InvalidArguments,
            [typeof(ModelBindingException)] = TgsApiError.InvalidArguments,
            [typeof(FormatException)] = TgsApiError.IncorrectFormat,
            [typeof(BrandUnderMaintenanceException)] = TgsApiError.UnderMaintenance,
            [typeof(InvalidStatusException)] = TgsApiError.InvalidStatus,
            [typeof(InvalidOperationException)] = TgsApiError.SystemError,
            [typeof(OperationNotAllowedException)] = TgsApiError.OperationNotAllowed,
            [typeof(GameProviderApiErrorException)] = TgsApiError.ProviderApiError,
            [typeof(Services.Operator.PlayerNotFoundException)] = TgsApiError.PlayerNotFound,
            [typeof(ValidateCodeNotVerifyException)] = TgsApiError.ValidateCodeNotVerify,
            [typeof(EmailNotVerifyException)] = TgsApiError.EmailNotVerify,
            [typeof(InsufficientBucketException)] = TgsApiError.InsufficientBucket,
            [typeof(CannotJoinPromotionException)] = TgsApiError.PromotionCannotJoin,
            [typeof(GameBalanceFundInFailedException)] = TgsApiError.GameBalanceFundInFailed,
            [typeof(GameBalanceFundOutFailedException)] = TgsApiError.GameBalanceFundOutFailed,
            [typeof(LoginFailException)] = TgsApiError.LoginFail,
            [typeof(FavoriteCountFullException)] = TgsApiError.FavoriteFull,
            [typeof(UserBannedException)] = TgsApiError.UserBanned,
            [typeof(BrandEmailDuplicateException)] = TgsApiError.BrandEmailDuplicate,
            [typeof(MaticPayDepositNeedRealNameException)] = TgsApiError.MaticPayDepositNeedRealName,
            [typeof(PlayerPhoneRequiredException)] = TgsApiError.PhoneNumberRequired,
            [typeof(PlayerCanNotWithdrawException)] = TgsApiError.PlayerCanNotWithdraw,
            [typeof(BrandPhoneDuplicateException)] = TgsApiError.BrandPhoneDuplicate,
            [typeof(ZeroNineNinePayDepositNeedRealNameException)] = TgsApiError.ZeroNineNinePayDepositNeedRealName,
            [typeof(PreviousDepositStillProcessingException)] = TgsApiError.PreviousDepositStillProcessing
        };

    public bool CanProcess(Exception exception)
        => true;

    public int GetApiErrorByException(Exception exception)
    {
        if (exception == null)
        {
            return (int)TgsApiError.None;
        }

        var type = exception.GetType();
        return _TgsApiErrorByException.TryGetValue(type, out var error)
            ? (int)error
            : (int)TgsApiError.SystemError;
    }

    public int GetHttpStatusByException(Exception exception)
    {
        if (exception == null)
        {
            return StatusCodes.Status200OK;
        }

        var type = exception.GetType();
        return _TgsApiErrorByException.TryGetValue(type, out var error) &&
            _HttpCodeByException.TryGetValue(error, out var statusCode)
                ? statusCode
                : StatusCodes.Status500InternalServerError;
    }

    public string GetMessageByException(Exception exception)
        => exception.Message;

    public string GetMessageKeyByException(Exception exception)
    {
        var type = exception.GetType();
        if (_TgsApiErrorByException.TryGetValue(type, out var errorType)
            && _ErrorCodeMessageIdMap.TryGetValue(errorType, out var messageKey))
            return messageKey;
        return _ErrorCodeMessageIdMap[TgsApiError.SystemError];
    }
}
