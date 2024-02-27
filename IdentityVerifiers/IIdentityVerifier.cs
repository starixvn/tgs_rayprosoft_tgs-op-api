namespace TGS.IdentityVerifiers;

public interface IIdentityVerifier
{
	bool Verify(string id);
}