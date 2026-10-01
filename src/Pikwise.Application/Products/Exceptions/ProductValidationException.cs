namespace Pikwise.Application.Products.Exceptions;

public sealed class ProductValidationException(IDictionary<string, string[]> errors)
    : Exception("Product validation failed.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
