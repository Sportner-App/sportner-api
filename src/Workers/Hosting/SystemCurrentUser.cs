using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Workers.Hosting;

/// <summary>
/// Background hosts run without a request principal; auditing records these writes as system-owned.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? UserId => null;

    public bool IsAuthenticated => false;
}
