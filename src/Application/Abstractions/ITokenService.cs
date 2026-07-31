using Sportner.Domain.Entities;

namespace Sportner.Application.Abstractions;

public interface ITokenService
{
    string CreateToken(User user);
}
