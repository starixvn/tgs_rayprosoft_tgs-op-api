using System.Collections.Immutable;
using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.BrandCertificate;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Exceptions;
using TGS.IdentityVerifiers;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Brand;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ApplicationServices.Withdraw;
using TGS.Services.Operator.PlayerRegisterFlow;

using CoreExceptions = TGS.Core.Models.Exceptions;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class PlayerController : ControllerBase
{
	private readonly ILogger<PlayerController> _logger;

	public PlayerController(ILogger<PlayerController> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Player changes password, player login is needed when use this
	/// </summary>
	[HttpPut("password", Name = "ChangePassword")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task<ChangePasswordResponse> ChangePasswordAsync(
		[FromServices] IGetService<ChangePasswordQuery, ChangePasswordInfo> getChangePasswordQueryService,
		[FromBody] ChangePasswordRequest request)
	{
		var brandId = HttpContext.GetBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new ChangePasswordQuery(
			brandId,
			playerTokenData.PlayerCode,
			request.OldPassword,
			request.NewPassword);

		ChangePasswordInfo info;

		try
		{
			info = await getChangePasswordQueryService
				.GetAsync(query)
				.ConfigureAwait(false);
		}
		catch (InvalidCredentialsException)
		{
			throw new InvalidCredentialsException("Password is mismatched.");
		}

		return new ChangePasswordResponse
		{
			PlayerCode = info.PlayerCode,
			UpdateTime = info.UpdateTime
		};
	}

	/// <summary>
	/// Checks the identity is valid.
	/// </summary>
	/// <param name="identityVerifierSelector">The identity verifier selector.</param>
	/// <param name="playerOperations">The player operations.</param>
	/// <param name="id">The identify number.</param>
	/// <returns></returns>
	[HttpGet("identity/check/{id}", Name = "CheckIdentityIsValid")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<bool> CheckIdentityIsValidAsync(
		[FromServices] IIdentityVerifierSelector<string> identityVerifierSelector,
		[FromServices] IPlayerOperations playerOperations,
		string id)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var player = await playerOperations
			.GetPlayerByCodeAsync(playerTokenData.PlayerCode)
			.ConfigureAwait(false);

		var identityVerifier = identityVerifierSelector.GetIdentityVerifier(player.DefaultWalletCurrencyCode);

		return identityVerifier.Verify(id);
	}

	[HttpPost("phone/check")]
	public ValueTask<bool> CheckPhoneCanUseAsync(
			[FromServices] IGetService<CheckBrandPhoneCanUseQuery, bool> checkService,
			[FromBody] StartSetPhoneRequest viewModel)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new CheckBrandPhoneCanUseQuery(
			brandId,
			null,
			viewModel.AreaCode,
			viewModel.Phone);

		return checkService.GetAsync(query, HttpContext.RequestAborted);
	}

	/// <summary>
	/// Check username exists
	/// </summary>
	[HttpGet("checkUsername", Name = "CheckUserNameExists")]
	public async Task<CheckUsernameResponse> CheckUsernameAsync(
		[FromServices] IGetService<GetUserNameQuery, bool> getUserNameQueryService,
		[FromQuery] CheckUsernameRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetUserNameQuery(brandId, request.Username);
		var isExist = await getUserNameQueryService.GetAsync(query).ConfigureAwait(false);

		return new CheckUsernameResponse { Exists = isExist };
	}

	/// <summary>
	/// Create default withdraw bankinfo of register flow.
	/// </summary>
	/// <param name="executeFlowService">The execute flow service.</param>
	/// <param name="viewModel">The view model.</param>
	/// <returns>
	///   <br />
	/// </returns>
	[HttpPost("init-bankinfo", Name = "CreatePlayerBankInfoOfPlayerRegister")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	public async Task CreateBankInfoAsync(
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		CreateInitBankInfoViewModel viewModel)
	{
		var brandId = HttpContext.GetBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			null);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			"SetWithdrawBankInfo",
			JsonSerializer.SerializeToDocument(viewModel),
			locationInfo);

		_ = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}

	[HttpPost("identity/upload", Name = "CreateIdentityAuthenticationForm")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask CreateIdentityAuthenticationFormAsync(
		[FromServices] IGetService<UploadIdentityCommand, string> uploadIdentityCommandService,
		[FromServices] ICommandService<CreateIdentityAuthenticationFormCommand> commandService,
		[FromForm(Name = "file")] IFormFile identityFile)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var contentType = identityFile.ContentType;
		await using var fileStream = identityFile.OpenReadStream();

		var fileId = await uploadIdentityCommandService
			.GetAsync(new UploadIdentityCommand(contentType, fileStream))
			.ConfigureAwait(false);

		var command = new CreateIdentityAuthenticationFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			fileId);

		await commandService
			.ExecuteAsync(command)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Start forgot password flow.
	/// </summary>
	/// <param name="forgotPasswordService">The forgot password service.</param>
	/// <param name="forgotPasswordBySmsService">The forgot password by SMS service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	/// <response code="200">Return the token, when reset password, must input to verify.</response>
	[HttpPost("password/forgot", Name = "ForgotPassword")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
	public ValueTask<string> ForgotPasswordAsync(
		[FromServices] IGetService<ForgotPasswordByMailCommand, string> forgotPasswordService,
		[FromServices] IGetService<ForgotPasswordBySmsCommand, string> forgotPasswordBySmsService,
		[FromBody] ForgotPasswordRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();

		return !string.IsNullOrWhiteSpace(request.Phone)
			? forgotPasswordBySmsService.GetAsync(new ForgotPasswordBySmsCommand(
				brandId,
				CultureInfo.CurrentUICulture,
				request.AreaCode,
				request.Phone))
			: forgotPasswordService.GetAsync(new ForgotPasswordByMailCommand(
			brandId,
			CultureInfo.CurrentUICulture,
			request.Email));
	}

	[HttpGet("game/auto-transfer", Name = "GetAutoTransferToGameFlag")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask<bool> GetAutoTransferToGameFlagAsync(
		[FromServices] IGetService<GetPlayerIsAutoTransferToGameQuery, bool> getService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetPlayerIsAutoTransferToGameQuery(playerTokenData.PlayerCode);

		return getService.GetAsync(query);
	}

	[HttpGet("register", Name = "GetCurrentRegisterStep")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	public ValueTask<string> GetCurrentRegisterStepAsync(
			[FromServices] IGetService<GetCurrentPlayerRegisterFlowStepQuery, string> getService)
	{
		var brandId = HttpContext.GetBrandId() ?? throw new BrandNotFoundException();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetCurrentPlayerRegisterFlowStepQuery(brandId, playerTokenData?.PlayerCode);

		return getService.GetAsync(query, HttpContext.RequestAborted);
	}

	/// <summary>
	/// Get player information
	/// </summary>
	[HttpGet("info", Name = "GetPlayerInfo")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	public async ValueTask<PlayerInfoViewModel> GetPlayerAsync(
		[FromServices] IGetService<GetPlayerMainDataQuery, Tgs.PlayerService.Client.PlayerInfo> getPlayerMainDataQueryService,
		[FromServices] IGetService<GetPlayerStatusQuery, Tgs.PlayerService.Client.PlayerStatus> getPlayerStatusQueryService,
		[FromServices] IGetService<GetPlayerVipLevelQuery, int> getPlayerVipLevelQueryService,
		[FromServices] IGetService<GetPlayerPropertiesQuery, IImmutableDictionary<string, string>> getPlayerPropertiesQueryService,
		[FromServices] IGetService<GetPlayerIsAutoTransferToGameQuery, bool> getAutoTransFlagService,
		[FromServices] IGetService<GetPlayerIdentityFormQuery, PlayerIdentityFormInfo> getIdentityFormStatusService,
		[FromServices] IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo> getBankInfoQueryService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var playerCode = playerTokenData.PlayerCode;

		var player = await getPlayerMainDataQueryService
			.GetAsync(new GetPlayerMainDataQuery(playerCode))
			.ConfigureAwait(false);

		if (player == null)
			throw new CoreExceptions.PlayerNotFoundException(playerCode, "Player not found.");

		var vipLevel = await getPlayerVipLevelQueryService
			.GetAsync(new GetPlayerVipLevelQuery(player.VipLevelId))
			.ConfigureAwait(false);

		var playerProperties = await getPlayerPropertiesQueryService
			.GetAsync(new GetPlayerPropertiesQuery(playerCode))
			.ConfigureAwait(false);

		var isAutoTransferToGame = await getAutoTransFlagService
			.GetAsync(new GetPlayerIsAutoTransferToGameQuery(playerCode))
			.ConfigureAwait(false);

		var identityFormInfo = await getIdentityFormStatusService
			.GetAsync(new GetPlayerIdentityFormQuery(playerCode))
			.ConfigureAwait(false);

		var birthdateValue = playerProperties.GetValueOrDefault("Birthdate");

		var playerStatus = await getPlayerStatusQueryService
			.GetAsync(new GetPlayerStatusQuery(playerCode), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var bankInfoAuthentication = playerStatus == Tgs.PlayerService.Client.PlayerStatus.Enabled
			  || await getBankInfoQueryService
				  .QueryAsync(new GetPlayerBankInfosQuery(playerCode))
				  .AnyAsync(info => info.IsDefault).ConfigureAwait(false);

		var playerInfo = new PlayerInfoViewModel
		{
			PlayerCode = player.PlayerCode,
			IdentityAuthentication = player.IdentityAuthentication,
			IdentityAuthenticationFormStatus = player.IdentityAuthentication ? PlayerIdentityReviewState.Approve : identityFormInfo.Status,
			IdentityAuthenticationFormId = identityFormInfo.FormId,
			IsAutoTransferToGame = isAutoTransferToGame,
			IsSetPayPassword = player.IsSetPayPassword,
			Username = player.UserName,
			Birthdate = string.IsNullOrWhiteSpace(birthdateValue) ? null : DateOnly.ParseExact(birthdateValue, "MM/dd/yyyy"),
			VipLevel = vipLevel,
			RealName = playerProperties.GetValueOrDefault("RealName"),
			Email = playerProperties.GetValueOrDefault("Email"),
			Phone = playerProperties.GetValueOrDefault("Phone"),
			PhoneAreaCode = playerProperties.GetValueOrDefault("AreaCode"),
			QQ = playerProperties.GetValueOrDefault("QQ"),
			Wechat = playerProperties.GetValueOrDefault("Wechat"),
			FacebookMessenger = playerProperties.GetValueOrDefault("FacebookMessenger"),
			Line = playerProperties.GetValueOrDefault("Line"),
			Viber = playerProperties.GetValueOrDefault("Viber"),
			Telegram = playerProperties.GetValueOrDefault("Telegram"),
			WhatsApp = playerProperties.GetValueOrDefault("WhatsApp"),
			BankInfoAuthentication = bankInfoAuthentication,
			EmailAuthentication = player.EmailAuthentication,
			PhoneAuthentication = player.PhoneAuthentication,
			IsCompletedRegister = playerStatus != Tgs.PlayerService.Client.PlayerStatus.RegisterUndone
		};

		return playerInfo;
	}

	/// <summary>
	/// Player login
	/// </summary>
	[HttpPost("login", Name = "Login")]
	[Consumes(MediaTypeNames.Application.Json)]
	[IgnoreUserBlacklistCheck]
	public async Task<LoginResponse> LoginAsync(
		[FromServices] IGetService<LoginCommand, LoginInfo> loginService,
		[FromBody] LoginRequest request)
	{
		var brandId = HttpContext.GetBrandId()!.Value;

		try
		{
			var ipAddress = HttpContext.Connection.RemoteIpAddress;
			var hostName = Request.Host.Host;

			var command = new LoginCommand(
				brandId,
				request.Username,
				request.Password,
				ipAddress.ToString(),
				hostName,
				request.PlatformType, Consts.Site);

			var info = await loginService
				.GetAsync(command)
				.ConfigureAwait(false);

			return new LoginResponse
			{
				LoginTime = info.LoginTime,
				PlayerToken = info.PlayerToken
			};
		}
		catch (Exception ex)
		{
			switch (ex)
			{
				case CoreExceptions.PlayerNotFoundException playerNotFoundException:
					_logger.LogWarning(playerNotFoundException, "{request.Username} not found.", request.Username);
					break;

				case InvalidCredentialsException invalidCredException:
					_logger.LogWarning(invalidCredException, "Password is mismatched.");
					break;

				default:
					_logger.LogWarning(ex, "Login fail!");
					break;
			}

			throw new LoginFailException();
		}
	}

	/// <summary>
	/// Player logout, player login is needed when use this
	/// </summary>
	[HttpDelete("logout", Name = "Logout")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	public async Task<LogoutResponse> LogoutAsync(
		[FromServices] IGetService<GetLogoutQuery, LogoutInfo> getLogoutQueryService)
	{
		var brandId = HttpContext.GetBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetLogoutQuery(brandId, playerTokenData.PlayerCode, playerTokenData.LoginRecordId);
		LogoutInfo info;
		try
		{
			info = await getLogoutQueryService
				.GetAsync(query)
				.ConfigureAwait(false);
		}
		catch (CoreExceptions.PlayerNotFoundException)
		{
			throw new CoreExceptions.PlayerNotFoundException(playerTokenData.PlayerCode, "Player not found.");
		}

		return new LogoutResponse { UpdateTime = info.UpdateTime };
	}

	/// <summary>
	/// Create a player
	/// </summary>
	[HttpPost("create", Name = "RegisterPlayer")]
	[Consumes(MediaTypeNames.Application.Json)]
	[Obsolete("已過時")]
	public async Task<CreatePlayerResponse> RegisterPlayerAsync(
		[FromServices] IGetService<BrandQuery, BrandInfo> getBrandService,
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		[FromHeader(Name = "Agent-Invitation-Code")] string invitationCode,
		[FromBody] CreatePlayerRequest request)
	{
		var brandId = HttpContext.GetBrandId() ?? throw new BrandNotFoundException();

		var brand = await getBrandService
			.GetAsync(new BrandQuery(brandId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		if (brand == null)
			throw new InvalidStatusException("Brand doesn't exist");

		if (brand.Status == BrandStatus.Maintenance)
			throw new BrandUnderMaintenanceException("Brand is put into maintenance");

		if (brand.Status != BrandStatus.Active)
			throw new InvalidStatusException("Brand is not active");

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			invitationCode);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			"CreatePlayer",
			JsonSerializer.SerializeToDocument(request),
			locationInfo);

		var result = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return result.StepResponse.Deserialize<CreatePlayerResponse>();
	}

	[HttpPost("register", Name = "RegisterPlayerFlow")]
	[IgnoreUserBlacklistCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = true)]
	public async Task<PlayerRegisterFlowResponseViewModel> RegisterPlayerAsync(
		[FromServices] IGetService<BrandQuery, BrandInfo> getBrandService,
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		[FromHeader(Name = "Agent-Invitation-Code")] string invitationCode,
		[FromBody] PlayerRegisterFlowViewModel model)
	{
		var brandId = HttpContext.GetBrandId() ?? throw new BrandNotFoundException();

		var brand = await getBrandService
			.GetAsync(new BrandQuery(brandId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		if (brand == null)
			throw new InvalidStatusException("Brand doesn't exist");

		if (brand.Status == BrandStatus.Maintenance)
			throw new BrandUnderMaintenanceException("Brand is put into maintenance");

		if (brand.Status != BrandStatus.Active)
			throw new InvalidStatusException("Brand is not active");

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			invitationCode);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			model.Type,
			model.Input,
			locationInfo);

		var result = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new PlayerRegisterFlowResponseViewModel
		{
			IsCompleted = result.IsCompleted,
			StepResponse = result.StepResponse,
			NextStepType = result.NextStepType
		};
	}

	/// <summary>
	/// Resets the password.
	/// </summary>
	/// <param name="resetService">The reset service.</param>
	/// <param name="request">The request.</param>
	/// <response code="406">Reset password not accept, maybe validate code not match.</response>
	[HttpPost("password/reset", Name = "ResetPassword")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status406NotAcceptable)]
	public async ValueTask ResetPasswordAsync(
		[FromServices] ICommandService<ResetPasswordCommand> resetService,
		[FromBody] ResetPasswordRequest request)
		=> await resetService
			.ExecuteAsync(new ResetPasswordCommand(request.SecurityKey, request.ValidateCode, request.NewPassword))
			.ConfigureAwait(false);

	/// <summary>
	/// Sets the birthdate asynchronous.
	/// </summary>
	/// <param name="setBirthdateService">The set birthdate service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPatch("birthdate", Name = "SetBirthdate")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask SetBirthdateAsync(
		[FromServices] ICommandService<SetBirthdateCommand> setBirthdateService,
		[FromBody] SetBirthdateRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetBirthdateCommand(playerTokenData.PlayerCode, request.Birthdate);

		return setBirthdateService.ExecuteAsync(command);
	}

	/// <summary>
	/// Sets the Facebook Messenger id.
	/// </summary>
	/// <param name="setFacebookMessengerService">The set facebook messenger service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("fb", Name = "SetFacebookMessengerId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SetFacebookMessengerIdAsync(
		[FromServices] ICommandService<SetFacebookMessengerCommand> setFacebookMessengerService,
		[FromBody] SetFacebookMessengerIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetFacebookMessengerCommand(playerTokenData.PlayerCode, request.FacebookMessenger);

		await setFacebookMessengerService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the Line id.
	/// </summary>
	/// <param name="setLineService">The set line service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("line", Name = "SetLineId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SetLineIdAsync(
		[FromServices] ICommandService<SetLineCommand> setLineService,
		[FromBody] SetLineIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetLineCommand(playerTokenData.PlayerCode, request.LineId);

		await setLineService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the player identity.
	/// </summary>
	/// <param name="setIdentityService">The set identity service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPatch("identity", Name = "SetPlayerIdentity")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask SetPlayerIdentityAsync(
		[FromServices] ICommandService<SetIdentityCommand> setIdentityService,
		[FromBody] SetPlayerIdentityRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetIdentityCommand(playerTokenData.PlayerCode, request.RealName, request.Identity, brandId);

		return setIdentityService.ExecuteAsync(command);
	}

	/// <summary>
	/// Sets the QQ number.
	/// </summary>
	/// <param name="setQqService">The set qq service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("qq", Name = "SetQqId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task SetQqIdAsync(
		[FromServices] ICommandService<SetQqCommand> setQqService,
		[FromBody] SetQqIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetQqCommand(playerTokenData.PlayerCode, request.QqId);

		await setQqService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the Telegram number.
	/// </summary>
	/// <param name="setTelegramService">The set telegram service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("telegram", Name = "SetTelegramId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SetTelegramIdAsync(
		[FromServices] ICommandService<SetTelegramCommand> setTelegramService,
		[FromBody] SetTelegramIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetTelegramCommand(playerTokenData.PlayerCode, request.TelegramId);

		await setTelegramService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the Viber id.
	/// </summary>
	/// <param name="setViberService">The set viber service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("viber", Name = "SetViberId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SetViberIdAsync(
		[FromServices] ICommandService<SetViberCommand> setViberService,
		[FromBody] SetViberIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetViberCommand(playerTokenData.PlayerCode, request.ViberId);

		await setViberService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the WeChat id.
	/// </summary>
	/// <param name="setWeChatService">The set we chat service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("wechat", Name = "SetWeChatId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task SetWeChatIdAsync(
		[FromServices] ICommandService<SetWeChatCommand> setWeChatService,
		[FromBody] SetWeChatIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetWeChatCommand(playerTokenData.PlayerCode, request.WebChatId);

		await setWeChatService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Sets the WhatsApp id.
	/// </summary>
	/// <param name="setWhatsAppService">The set line service.</param>
	/// <param name="request">The request.</param>
	[HttpPatch("whatsapp", Name = "SetWhatsAppId")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SetWhatsAppIdAsync(
		[FromServices] ICommandService<SetWhatsAppCommand> setWhatsAppService,
		[FromBody] SetWhatsAppIdRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new SetWhatsAppCommand(playerTokenData.PlayerCode, request.WhatsAppId);

		await setWhatsAppService.ExecuteAsync(command).ConfigureAwait(false);
	}

	/// <summary>
	/// Starts the change email.
	/// </summary>
	/// <param name="setPlayerEmailService">The set player email service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPost("email", Name = "StartChangeEmail")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask<string> StartChangeEmailAsync(
		[FromServices] IGetService<StartSetPlayerEmailCommand, string> setPlayerEmailService,
		[FromBody] StartSetEmailRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new StartSetPlayerEmailCommand(
			playerTokenData.PlayerCode,
			request.Email,
			CultureInfo.CurrentUICulture,
			false);

		return setPlayerEmailService.GetAsync(command);
	}

	/// <summary>
	/// Starts the change phone.
	/// </summary>
	/// <param name="getBrandInfoService">The brand info query service.</param>
	/// <param name="checkService">The check player phone service.</param>
	/// <param name="startSetPlayerPhoneService">The start set player phone service.</param>
	/// <param name="request">The request.</param>
	[HttpPost("phone", Name = "StartChangePhone")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<string> StartChangePhoneAsync(
		[FromServices] IGetService<BrandQuery, BrandInfo> getBrandInfoService,
		[FromServices] IGetService<CheckBrandPhoneCanUseQuery, bool> checkService,
		[FromServices] IGetService<StartSetPlayerPhoneCommand, string> startSetPlayerPhoneService,
		[FromBody] StartSetPhoneRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Phone)
			|| string.IsNullOrWhiteSpace(request.AreaCode))
			throw new PlayerPhoneRequiredException();

		var brandId = HttpContext.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var brand = await getBrandInfoService.GetAsync(new BrandQuery(brandId!.Value)).ConfigureAwait(false);

		var checkQuery = new CheckBrandPhoneCanUseQuery(
			brand.BrandId,
			playerTokenData.PlayerCode,
			request.AreaCode.Trim(),
			request.Phone.Trim());

		var canDoCommand = await checkService
			.GetAsync(checkQuery)
			.ConfigureAwait(false);

		if (!canDoCommand)
			throw new BrandPhoneDuplicateException();

		var command = new StartSetPlayerPhoneCommand(
			playerTokenData.PlayerCode,
			request.AreaCode.Trim(),
			request.Phone.Trim(),
			CultureInfo.CurrentUICulture,
			brand.SmsEnabled);

		return await startSetPlayerPhoneService
			.GetAsync(command)
			.ConfigureAwait(false);
	}

	/// <summary>Starts the change email of register flow.</summary>
	/// <param name="executeFlowService"></param>
	/// <param name="request">The request.</param>
	/// <returns>
	///   <br />
	/// </returns>
	[HttpPost("init-email", Name = "StartSetEmailOfPlayerRegister")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	[Obsolete("已過時")]
	public async ValueTask<string> StartSetEmailAsync(
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		[FromBody] StartInitEmailRequest request)
	{
		var brandId = HttpContext.GetBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			null);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			"InitMail",
			JsonSerializer.SerializeToDocument(new
			{
				input = "Init",
				data = request
			}),
			locationInfo);

		var result = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return result.StepResponse.Deserialize<string>();
	}

	/// <summary>Starts to set phone with player token</summary>
	/// <param name="executeFlowService"></param>
	/// <param name="request">The request.</param>
	/// <returns>security code</returns>
	[HttpPost("init-phone", Name = "StartSetPhoneOfPlayerRegister")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	[Obsolete("已過時")]
	public async ValueTask<string> StartSetPhoneAsync(
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		[FromBody] StartInitPhoneRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Phone)
			|| string.IsNullOrWhiteSpace(request.AreaCode))
			throw new PlayerPhoneRequiredException();

		var brandId = HttpContext.GetBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			null);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			"InitPhone",
			JsonSerializer.SerializeToDocument(new
			{
				input = "Init",
				data = request
			}),
			locationInfo);

		var result = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return result.StepResponse?.Deserialize<string>();
	}

	[HttpPut("init-email", Name = "SubmitChangeEmailOfPlayerRegister")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	[Obsolete("已過時")]
	public async ValueTask SubmitChangeEmailAsync(
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		[FromBody] SubmitInitEmailRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			null);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			"InitMail",
			JsonSerializer.SerializeToDocument(new
			{
				input = "Valid",
				data = request
			}),
			locationInfo);

		_ = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Submits the change email.
	/// </summary>
	/// <param name="commitPlayerEmailService">The commit player email service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPut("email", Name = "SubmitChangeEmail")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask SubmitChangeEmailAsync(
		[FromServices] ICommandService<CommitSetPlayerEmailCommand> commitPlayerEmailService,
		[FromBody] SubmitChangeEmailRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var command = new CommitSetPlayerEmailCommand(
			playerTokenData.PlayerCode,
			request.SecurityKey,
			request.ValidateCode,
			brandId);

		return commitPlayerEmailService.ExecuteAsync(command);
	}

	/// <summary>
	/// Submits the change phone.
	/// </summary>
	/// <param name="commitPlayerPhoneService">The commit player phone service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPut("phone", Name = "SubmitChangePhone")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SubmitChangePhoneAsync(
		[FromServices] ICommandService<CommitSetPlayerPhoneCommand> commitPlayerPhoneService,
		[FromBody] SubmitChangePhoneRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new CommitSetPlayerPhoneCommand(
			brandId,
			playerTokenData.PlayerCode,
			request.SecurityKey,
			request.ValidateCode);

		await commitPlayerPhoneService
			.ExecuteAsync(command)
			.ConfigureAwait(false);
	}

	/// <summary>Submits the change phone.</summary>
	/// <param name="executeFlowService"></param>
	/// <param name="request">The request.</param>
	/// <returns>
	///   <br />
	/// </returns>
	[HttpPut("init-phone", Name = "SubmitChangePhoneOfPlayerRegister")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	[Obsolete("已過時")]
	public async ValueTask SubmitChangePhoneAsync(
		[FromServices] IGetService<ExecutePlayerRegisterFlowCommand, RegisterFlowProcessResult> executeFlowService,
		[FromBody] SubmitInitPhoneRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var ipAddress = HttpContext.Connection.RemoteIpAddress;
		var hostName = Request.Host.Host;

		var locationInfo = new PlayerLocationInfo(
			playerTokenData?.PlayerCode,
			ipAddress.ToString(),
			hostName,
			null);

		var command = new ExecutePlayerRegisterFlowCommand(
			brandId,
			"Init-Phone",
			JsonSerializer.SerializeToDocument(new
			{
				input = "Valid",
				data = request
			}),
			locationInfo);

		_ = await executeFlowService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}

	[HttpPost("game/auto-transfer", Name = "ToggleAutoTransferToGameFlag")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask ToggleAutoTransferToGameFlagAsync(
		[FromServices] ICommandService<TogglePlayerIsAutoTransferToGameCommand> toggleService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new TogglePlayerIsAutoTransferToGameCommand(playerTokenData.PlayerCode);

		return toggleService.ExecuteAsync(command);
	}
}
