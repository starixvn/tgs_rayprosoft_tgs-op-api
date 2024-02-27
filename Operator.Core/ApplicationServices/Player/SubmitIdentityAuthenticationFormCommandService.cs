using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgs.FileService.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Utility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SubmitIdentityAuthenticationFormCommandService(IFileServiceClient fileServiceClient) : ICommandService<SubmitIdentityAuthenticationFormCommand>
{
	public async ValueTask ExecuteAsync(SubmitIdentityAuthenticationFormCommand command, CancellationToken cancellationToken = default)
	{
		var randomFileName = StringGenerator.GenerateAlphanumericString(32);

		_ = await fileServiceClient
			.UploadFileAsync(
				$"/{randomFileName}",
				MediaTypeHeaderValue.Parse(command.ContentType),
				command.FileStream)
			.ConfigureAwait(false);
	}
}