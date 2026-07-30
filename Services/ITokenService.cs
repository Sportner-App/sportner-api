using SportnerApi.Models;

namespace SportnerApi.Services;

public interface ITokenService
{
    string CreateToken(Profile user);
}
