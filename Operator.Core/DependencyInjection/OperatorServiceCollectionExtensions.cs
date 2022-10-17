using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using TGS.Core.Infrastructures.Mail;
using TGS.Core.Infrastructures.WebApiServices;
using TGS.Services.Operator;
using TGS.Services.Operator.Configuration;
using TGS.Services.Operator.PlayerRegisterFlow;
using TGS.Services.Operator.ValidateCode;

namespace Microsoft.Extensions.DependencyInjection;

public static class OperatorServiceCollectionExtensions
{
	private static readonly Dictionary<string, Type> _FlowTypes = new()
	{
		["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
		["InitMail"] = typeof(SetEmailRegisterFlowStep),
		["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep),
		["SetPhoneDontCareDuplicate"] = typeof(SetPhoneDontCareDuplicateRegisterFlowStep),
		["SetEmailDontCareDuplicate"] = typeof(SetEmailDontCareDuplicateRegisterFlowStep),
		["SetRealName"] = typeof(SetRealNameRegisterFlowStep),
		["JoinPromotion"] = typeof(SetPromotionJoinRegisterFlowStep)
	};

	public static OperatorConfiguration AddOperatorApiCore(this IServiceCollection services)
	{
		_ = services.RegisterAllServiceUtilityTypes(
			typeof(OperatorServiceCollectionExtensions).Assembly,
			ServiceLifetime.Transient);

		_ = services
			.AddOptions<OperatorOptions>()
			.Services
			.AddValidateCodeClient((options, sp) =>
			{
				var operatorOptions = sp.GetRequiredService<IOptions<OperatorOptions>>().Value;

				options.ValidateCodeBaseUrl = operatorOptions.ValidateCodeBaseUrl;
			})
			.AddTransient<IGameProviderStatusValidator, GameProviderStatusValidator>()
			.AddTransient<IValidateCodeOperator, RestValidateCodeOperator>()
			.AddTransient<IRequestAnalyzer, RequestAnalyzer>()
			.AddSingleton<IValidateCodeGenerator, RandomValidateCodeGenerator>()
			.AddMail()
			.AddSmsClient((sp, httpClient) =>
			{
				var options = sp.GetRequiredService<IOptions<OperatorOptions>>().Value;

				httpClient.BaseAddress = options.SmsBaseUrl;
			})
			.AddFileServiceClient((sp, httpClient) =>
			{
				var options = sp.GetRequiredService<IOptions<OperatorOptions>>().Value;

				httpClient.BaseAddress = options.FileServiceBaseUrl;
			})
			.AddPaymentClient()
			.AddRouletteSignInClient()
			.AddDepositAccumulateRouletteClient();

		_ = services
			.AddScoped(sp => new RegisterFlowBuilder(
				sp,
				_FlowTypes.ToImmutableDictionary()))
			.AddScoped<IBrandRegisterFlowDefinitionManager, BrandFlowDefinitionManager>()
			.AddScoped<CreatePlayerRegisterFlowStep>()
			.AddScoped<FinalRegisterFlowStep>();

		foreach (var v in _FlowTypes.Values)
			_ = services.AddScoped(v);

		var opConfiguration = new OperatorConfiguration(services);

		return opConfiguration;
	}
}
