using MediatR;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
