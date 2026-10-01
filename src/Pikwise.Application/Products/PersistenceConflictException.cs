namespace Pikwise.Application.Products;

public sealed class PersistenceConflictException(Exception innerException)
    : Exception("The data changed or conflicts with a related record. Reload and retry.", innerException);
