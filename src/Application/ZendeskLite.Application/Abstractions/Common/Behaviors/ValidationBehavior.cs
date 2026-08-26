using FluentValidation;
using MediatR;
using ZendeskLite.Domain.Common;
using System.Reflection;

namespace ZendeskLite.Application.Abstractions.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var validationTasks = _validators.Select(v => v.ValidateAsync(context, ct));
        var validationResults = await Task.WhenAll(validationTasks);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            return CreateValidationResult<TResponse>(failures);
        }

        return await next();
    }

    private static TResult CreateValidationResult<TResult>(List<FluentValidation.Results.ValidationFailure> failures)
        where TResult : Result
    {
        var error = Error.Validation(failures.First().PropertyName, failures.First().ErrorMessage);

        // Check if the TResult is a generic Result<T>
        if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResult).GetGenericArguments()[0];

            // Target ValidationFailure instead of Failure to avoid overload ambiguity
            var method = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == nameof(Result.ValidationFailure)
                                   && m.IsGenericMethod
                                   && m.GetGenericArguments().Length == 1)!;

            return (TResult)method.MakeGenericMethod(valueType).Invoke(null, new object[] { error })!;
        }

        // Target ValidationFailure instead of Failure here too
        var nonGenericMethod = typeof(Result)
            .GetMethod(nameof(Result.ValidationFailure), new[] { typeof(Error) })!;

        return (TResult)nonGenericMethod.Invoke(null, new object[] { error })!;
    }
}