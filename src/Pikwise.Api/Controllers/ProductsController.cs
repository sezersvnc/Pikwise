using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Interfaces;

namespace Pikwise.Api.Controllers;

[ApiController]
[Route("api/products")]
// Keep HTTP routing and status codes here; ProductService handles each use case.
// Reads are public; writes need a local Admin profile (LocalAdmin policy, ADR-029).
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet("compare")]
    [ProducesResponseType<ProductComparisonResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductComparisonResponseDto>> Compare(
        [FromQuery] ProductComparisonRequestDto request, CancellationToken cancellationToken) =>
        Ok(await productService.CompareAsync(request, cancellationToken));

    [HttpGet]
    [ProducesResponseType<PagedProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedProductResponseDto>> GetAll(
        [FromQuery] ProductQueryRequestDto query, CancellationToken cancellationToken) =>
        Ok(await productService.GetAllAsync(query, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "LocalAdmin")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequestDto request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        // Include the new resource's GET URL in the 201 response's Location header.
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "LocalAdmin")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponseDto>> Update(int id, UpdateProductRequestDto request, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(id, request, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "LocalAdmin")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await productService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetById(
        int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }
}
