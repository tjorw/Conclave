namespace ConventionSystem.Application.Common;

public interface ICommand<TResult> : IRequest<TResult> { }

public interface ICommand : ICommand<Unit> { }
