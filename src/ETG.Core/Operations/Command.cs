using MediatR;

namespace ETG.Core.Operations;

public interface ICommand<out TResult> : IRequest<TResult> { }