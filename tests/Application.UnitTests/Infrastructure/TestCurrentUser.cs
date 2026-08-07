using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Application.UnitTests.Infrastructure;

internal sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(Guid? userId) => UserId = userId;

    public Guid? UserId { get; }

    public bool IsAuthenticated => UserId is not null;
}
