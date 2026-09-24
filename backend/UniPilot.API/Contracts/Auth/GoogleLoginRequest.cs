namespace UniPilot.API.Contracts.Auth;

public sealed class GoogleLoginRequest
{
    public string Credential { get; set; } =
        string.Empty;
}
