using FluentValidation;
using MediatR;
using ParkShare.Application.Dto;

namespace ParkShare.Application.Infrastructure
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
            => _validators = validators;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                // Construct a failed ServiceResult (must match TResponse structure)
                var errorMessages = failures.Select(f => f.ErrorMessage).ToList();

                var resultType = typeof(ServiceResult<>).MakeGenericType(typeof(object)); // fallback
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(ServiceResult<>))
                {
                    resultType = typeof(ServiceResult<>).MakeGenericType(typeof(TResponse).GetGenericArguments()[0]);
                }

                var failedResult = resultType
                    .GetMethod("WithErrors", new[] { typeof(List<string>) })!
                    .Invoke(null, new object[] { errorMessages });

                return (TResponse)failedResult!;
            }

            return await next();
        }
    }
}
