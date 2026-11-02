using MediatR;
using Microsoft.AspNetCore.Mvc;
using SalesSample.Application.Sales.Commands.CancelSale;
using SalesSample.Application.Sales.Commands.CreateSale;
using SalesSample.Application.Sales.Queries.GetSale;

namespace SalesSample.WebApi.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        var command = new CreateSaleCommand(
            request.CustomerId,
            request.BranchId,
            request.SaleNumber,
            request.Items.Select(i => new CreateSaleItem(i.ProductId, i.Quantity)).ToList());

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelSaleCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSaleQuery(id), ct);
        return Ok(result);
    }
}

public sealed record CreateSaleRequest(
    Guid CustomerId,
    Guid BranchId,
    string? SaleNumber,
    IReadOnlyCollection<CreateSaleItemRequest> Items);

public sealed record CreateSaleItemRequest(Guid ProductId, int Quantity);