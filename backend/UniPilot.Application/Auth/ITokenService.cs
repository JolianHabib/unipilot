namespace UniPilot.Application.Auth;

public interface ITokenService
{
    TokenResult CreateToken(
        Guid userId,
        string fullName,
        string email);
}