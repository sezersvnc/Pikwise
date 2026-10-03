namespace Pikwise.Application.Products.DTOs;

public sealed record PagedProductResponseDto(
    IReadOnlyList<ProductResponseDto> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)(((long)TotalCount + PageSize - 1) / PageSize);
    public bool HasPreviousPage => TotalPages > 0 && Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
