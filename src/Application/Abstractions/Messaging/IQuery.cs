using MediatR;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
