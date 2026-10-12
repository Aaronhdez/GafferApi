namespace Gaffer.Api.Queries;

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse> {
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}

public interface IQuery<out TResponse> { }
