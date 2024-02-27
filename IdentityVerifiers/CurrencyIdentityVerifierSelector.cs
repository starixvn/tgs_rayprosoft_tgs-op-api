namespace TGS.IdentityVerifiers;

public class CurrencyIdentityVerifierSelector : IIdentityVerifierSelector<string>
{
	public IIdentityVerifier GetIdentityVerifier(string currency) => currency switch
	{
		"CNY" => new ChineseIdentityVerifier(),
		_ => new SoutheastAsiaIdentityVerifier()
	};
}