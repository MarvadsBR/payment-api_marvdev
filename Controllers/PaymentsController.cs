using Microsoft.AspNetCore.Mvc;
using PaymentApi.DTOs;
using PaymentApi.Services;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt",
        "amount",
        "status",
        "method"
    };

    private static readonly HashSet<string> AllowedSortDir = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "desc"
    };

    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    /// <summary>
    /// List payments with optional filtering and pagination.
    /// </summary>
    /// <param name="status">Optional status filter (Pending, Completed, Failed, Refunded).</param>
    /// <param name="page">Page number, 1-based. Default: 1.</param>
    /// <param name="pageSize">Items per page (1–100). Default: 10.</param>
    /// <param name="sortBy">Sort field: createdAt, amount, status, method. Default: createdAt.</param>
    /// <param name="sortDir">Sort direction: asc or desc. Default: desc.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<PaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc")
    {
        if (page < 1)
            return BadRequest(new { error = "'page' must be greater than or equal to 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "'pageSize' must be between 1 and 100." });

        if (!AllowedSortBy.Contains(sortBy))
            return BadRequest(new { error = "'sortBy' must be one of: createdAt, amount, status, method." });

        if (!AllowedSortDir.Contains(sortDir))
            return BadRequest(new { error = "'sortDir' must be 'asc' or 'desc'." });

        var result = await _service.GetAllAsync(status, page, pageSize, sortBy, sortDir);
        return Ok(result);
    }

    /// <summary>Get a payment by its ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var payment = await _service.GetByIdAsync(id);
        return payment is null ? NotFound() : Ok(payment);
    }

    /// <summary>Create a new payment.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update the status of a payment.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePaymentStatusDto dto)
    {
        var updated = await _service.UpdateStatusAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a payment. Only payments with status Pending can be deleted.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        return result switch
        {
            DeleteResult.Success    => NoContent(),
            DeleteResult.NotFound   => NotFound(),
            DeleteResult.NotAllowed => Conflict(new { error = "Only payments with status 'Pending' can be deleted." }),
            _                       => StatusCode(500)
        };
    }
}
