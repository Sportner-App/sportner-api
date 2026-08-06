using MediatR;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
