using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Sportner.API.Common;

/// <summary>
/// Base type for all controllers. Keeps controllers thin: resolve <see cref="ISender"/> lazily
/// and dispatch requests through MediatR. Controllers must contain no business logic.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender =>
        _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
