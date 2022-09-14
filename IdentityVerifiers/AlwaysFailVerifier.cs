namespace TGS.IdentityVerifiers;

public class AlwaysFailVerifier : IIdentityVerifier
{
    public bool Verify(string id) => false;
}