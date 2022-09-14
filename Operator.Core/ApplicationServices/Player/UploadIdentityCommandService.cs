using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgs.FileService.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Utility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class UploadIdentityCommandService : IGetService<UploadIdentityCommand, string>
{
    private readonly IFileServiceClient _fileServiceClient;

    public UploadIdentityCommandService(IFileServiceClient fileServiceClient)
    {
        _fileServiceClient = fileServiceClient ?? throw new ArgumentNullException(nameof(fileServiceClient));
    }

    public async ValueTask<string> GetAsync(UploadIdentityCommand command, CancellationToken cancellationToken = default)
    {
        var randomFileName = StringGenerator.GenerateAlphanumericString(32);

        var uploadResult = await _fileServiceClient
            .UploadFileAsync(
                $"/{randomFileName}",
                MediaTypeHeaderValue.Parse(command.ContentType),
                command.FileStream)
            .ConfigureAwait(false);

        return uploadResult.Id;
    }
}
