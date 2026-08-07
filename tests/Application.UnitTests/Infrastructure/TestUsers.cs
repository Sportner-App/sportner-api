using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Infrastructure;

internal static class TestUsers
{
    internal static User CreateActive(string phoneNumber, DateTimeOffset utcNow)
    {
        var user = User.Create(phoneNumber, utcNow);
        user.VerifyPhoneNumber(utcNow);
        user.Activate(utcNow);
        return user;
    }
}
