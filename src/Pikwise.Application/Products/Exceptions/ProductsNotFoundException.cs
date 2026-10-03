namespace Pikwise.Application.Products.Exceptions;

public sealed class ProductsNotFoundException(IReadOnlyList<int> missingProductIds)
    : Exception("One or more requested products do not exist.")
{
    public IReadOnlyList<int> MissingProductIds { get; } = missingProductIds;
}
