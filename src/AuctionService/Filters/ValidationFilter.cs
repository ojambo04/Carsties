using System.ComponentModel.DataAnnotations;

namespace AuctionService.Filters;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Check all arguments for validation
        foreach (var arg in context.Arguments)
        {
            if (arg is null) continue;

            var validationResults = new List<ValidationResult>();
            var contextObj = new ValidationContext(arg);

            bool isValid = Validator.TryValidateObject(arg, contextObj, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                var errors = validationResults
                    .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage ?? "").ToArray()
                    );

                return Results.ValidationProblem(errors);
            }
        }

        // Continue pipeline if valid
        return await next(context);
    }
}