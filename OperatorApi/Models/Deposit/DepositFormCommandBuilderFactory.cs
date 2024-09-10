namespace TGS.Apis.OperatorApi.Models.Deposit;

public class DepositFormCommandBuilderFactory(IServiceProvider serviceProvider)
{
	public IDepositFormCommandBuilder CreateBuilder(string paymentCode)
	{
		return paymentCode switch
		{
			_ when string.Equals("U2Pay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<U2PayCommandBuilder>(serviceProvider),
			_ when string.Equals("DibaoPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<DibaoPayCommandBuilder>(serviceProvider),
			_ when string.Equals("GanyoPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GanyoPayCommandBuilder>(serviceProvider),
			_ when string.Equals("SBPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<SBPayCommandBuilder>(serviceProvider),
			_ when string.Equals("HPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GCPayCommandBuilder>(serviceProvider),
			_ when string.Equals("GCPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GCPayCommandBuilder>(serviceProvider),
			_ when string.Equals("PhpGoPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("MetaPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<MetaPayCommandBuilder>(serviceProvider),
			_ when string.Equals("AlfaPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<AlfaPayCommandBuilder>(serviceProvider),
			_ when string.Equals("FiveSevenPay-V2", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("MalakiPay-V2", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("WinPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("123Pay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("ZeroNineNinePay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<ZeroNineNinePayCommandBuilder>(serviceProvider),
			_ when string.Equals("WandaPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<WandaPayCommandBuilder>(serviceProvider),
			_ when string.Equals("UmPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<UmPayCommandBuilder>(serviceProvider),
			_ when string.Equals("TuPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<TuPayCommandBuilder>(serviceProvider),
			_ when string.Equals("SkyPay-VN", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<SkyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("SkyPay-CN", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<SkyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("PrimePay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<PrimePayCommandBuilder>(serviceProvider),
			_ when string.Equals("MaticPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<MaticPayCommandBuilder>(serviceProvider),
			_ when string.Equals("Help2Pay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<Help2PayCommandBuilder>(serviceProvider),
			_ when string.Equals("LeePay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<LeePayCommandBuilder>(serviceProvider),
			_ when string.Equals("UBPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<UBPayCommandBuilder>(serviceProvider),
			_ when string.Equals("FeibaoPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<FeibaoPayCommandBuilder>(serviceProvider),
			_ when string.Equals("LPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("SunnyPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GalaxyPayCommandBuilder>(serviceProvider),
			_ when string.Equals("EzPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GCPayCommandBuilder>(serviceProvider),
			_ when string.Equals("ShunfaPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<ShunfaPayCommandBuilder>(serviceProvider),
			_ when string.Equals("AlfaPay-IDR", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<AlfaPayCommandBuilder>(serviceProvider),
			_ when string.Equals("MegaPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<AlfaPayCommandBuilder>(serviceProvider),
			_ when string.Equals("HaloPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<HaloPayCommandBuilder>(serviceProvider),
			_ when string.Equals("FuKaiPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<FuKaiPayCommandBuilder>(serviceProvider),
			_ when string.Equals("S88Pay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<S88PayCommandBuilder>(serviceProvider),
			_ when string.Equals("DirePay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<DirePayCommandBuilder>(serviceProvider),
			_ when string.Equals("GameWallet", paymentCode, StringComparison.InvariantCultureIgnoreCase)
				=> ActivatorUtilities.GetServiceOrCreateInstance<GameWalletCommandBuilder>(serviceProvider),
			_ when string.Equals("SpeedPay", paymentCode, StringComparison.InvariantCultureIgnoreCase)
			=> ActivatorUtilities.GetServiceOrCreateInstance<SpeedPayCommandBuilder>(serviceProvider),
			_ => throw new NotImplementedException()
		};
	}
}