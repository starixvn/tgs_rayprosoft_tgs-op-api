namespace TGS.IdentityVerifiers;

public interface IIdentityVerifierSelector<TCondition>
{
    IIdentityVerifier GetIdentityVerifier(TCondition condition);
}