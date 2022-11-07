using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgs.FileService.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Utility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SubmitIdentityAuthenticationFormCommandService : ICommandService<SubmitIdentityAuthenticationFormCommand>
{
	private readonly IFileServiceClient _fileServiceClient;

	public SubmitIdentityAuthenticationFormCommandService(IFileServiceClient fileServiceClient)
	{
		_fileServiceClient = fileServiceClient;
	}

	public async ValueTask ExecuteAsync(SubmitIdentityAuthenticationFormCommand command, CancellationToken cancellationToken = default)
	{
		var randomFileName = StringGenerator.GenerateAlphanumericString(32);

		_ = await _fileServiceClient
			.UploadFileAsync(
				$"/{randomFileName}",
				MediaTypeHeaderValue.Parse(command.ContentType),
				command.FileStream)
			.ConfigureAwait(false);
	}
}
