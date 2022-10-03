using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Auth;

internal class MakeBrandAuthTokenCommandService : IGetService<MakeBrandAuthTokenCommand, AuthInfo>
{
    private readonly IBrandOperations _brandOperations;
    private readonly IJwtTokenHandler _jwtTokenHandler;

    public MakeBrandAuthTokenCommandService(IJwtTokenHandler jwtTokenHandler, IBrandOperations brandOperations)
    {
        _jwtTokenHandler = jwtTokenHandler ?? throw new ArgumentNullException(nameof(jwtTokenHandler));
        _brandOperations = brandOperations ?? throw new ArgumentNullException(nameof(brandOperations));
    }

    public ValueTask<AuthInfo> GetAsync(
        MakeBrandAuthTokenCommand query, CancellationToken cancellationToken = default)
    {
        var secretKey = "abcd";

        var currency = query.Identity.FindFirst("curr")!.Value;
        var signature = $"{query.BrandId}:{currency}:{secretKey}";
        var expires = DateTime.Now.AddSeconds(Consts.TokenExpireSeconds.Jwt);

        var token = _jwtTokenHandler.GenerateToken(
            query.Identity,
            Encoding.UTF8.GetBytes(signature),
            query.Site,
            expires);

        return ValueTask.FromResult(new AuthInfo(
            token,
            new DateTimeOffset(expires).ToUnixTimeMilliseconds()));
    }
}
