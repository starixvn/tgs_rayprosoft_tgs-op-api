using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record CheckUsernameRequest
{
    [BindProperty(Name = "username", SupportsGet = true)]
    public string Username { get; init; }
}