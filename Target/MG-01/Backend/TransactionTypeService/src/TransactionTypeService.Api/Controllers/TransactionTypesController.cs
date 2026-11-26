using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionTypeService.Api.Dtos;
using TransactionTypeService.Api.Security;
using TransactionTypeService.Api.Services;

namespace TransactionTypeService.Api.Controllers;

/// <summary>REST resource for transaction type reference data (legacy CTLI/CTTU).</summary>
[ApiController]
[Route("api/v1/transaction-types")]
[Produces("application/json")]
public class TransactionTypesController(ITransactionTypeService service) : ControllerBase
{
    private string CurrentUser => User.FindFirstValue("sub") ?? User.Identity?.Name ?? "unknown";

    /// <summary>Lists transaction types with optional filters and keyset paging (default 7 rows).</summary>
    /// <response code="200">One page of transaction types.</response>
    /// <response code="400">Invalid filter or paging parameters.</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType<TransactionTypePageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<TransactionTypePageDto> List([FromQuery] ListQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    /// <summary>Gets one transaction type by its 2-digit code.</summary>
    /// <response code="200">The transaction type.</response>
    /// <response code="404">No transaction type with this code.</response>
    [HttpGet("{typeCode}")]
    [Authorize(Policy = AuthorizationPolicies.Read)]
    [ProducesResponseType<TransactionTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TransactionTypeDto> Get(string typeCode, CancellationToken ct) => service.GetAsync(typeCode, ct);

    /// <summary>Creates a transaction type.</summary>
    /// <response code="201">Created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">The code already exists.</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType<TransactionTypeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionTypeDto>> Create([FromBody] CreateTransactionTypeRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, CurrentUser, ct);
        return Created($"/api/v1/transaction-types/{created.TypeCode}", created);
    }

    /// <summary>Changes the description of an existing transaction type.</summary>
    /// <response code="200">Updated.</response>
    /// <response code="404">No transaction type with this code.</response>
    /// <response code="409">The row is locked by another change.</response>
    /// <response code="422">The description is unchanged.</response>
    [HttpPut("{typeCode}")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType<TransactionTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<TransactionTypeDto> Update(string typeCode, [FromBody] UpdateTransactionTypeRequest request, CancellationToken ct) =>
        service.UpdateAsync(typeCode, request, CurrentUser, ct);

    /// <summary>Deletes a transaction type that has no dependent category rows.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No transaction type with this code.</response>
    /// <response code="409">Dependent records exist or the row is locked.</response>
    [HttpDelete("{typeCode}")]
    [Authorize(Policy = AuthorizationPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string typeCode, CancellationToken ct)
    {
        await service.DeleteAsync(typeCode, CurrentUser, ct);
        return NoContent();
    }
}
