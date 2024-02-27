using System.IO;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record UploadIdentityCommand(string ContentType, Stream FileStream);