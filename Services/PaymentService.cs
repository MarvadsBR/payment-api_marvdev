using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.DTOs;
using PaymentApi.Models;

namespace PaymentApi.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponseDto<PaymentResponseDto>> GetAllAsync(string? status, int page, int pageSize, string sortBy, string sortDir)
    {
        var query = _db.Payments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<PaymentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync();

        query = ApplyOrdering(query, sortBy, sortDir);

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();

        return new PagedResponseDto<PaymentResponseDto>
        {
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
            Data       = data
        };
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(Guid id)
    {
        var payment = await _db.Payments.FindAsync(id);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto> CreateAsync(CreatePaymentDto dto)
    {
        var payment = new Payment
        {
            Amount = dto.Amount,
            Currency = dto.Currency.ToUpperInvariant(),
            Method = dto.Method,
            Description = dto.Description,
            ExternalReference = dto.ExternalReference
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return ToDto(payment);
    }

    public async Task<PaymentResponseDto?> UpdateStatusAsync(Guid id, UpdatePaymentStatusDto dto)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment is null) return null;

        payment.Status = dto.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<DeleteResult> DeleteAsync(Guid id)
    {
        var payment = await _db.Payments.FindAsync(id);

        if (payment is null) return DeleteResult.NotFound;
        if (payment.Status != PaymentStatus.Pending) return DeleteResult.NotAllowed;

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();

        return DeleteResult.Success;
    }

    private static IQueryable<Payment> ApplyOrdering(IQueryable<Payment> query, string sortBy, string sortDir)
    {
        var desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "amount" => desc
                ? query.OrderByDescending(p => p.Amount).ThenByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.Amount).ThenByDescending(p => p.CreatedAt),

            "status" => desc
                ? query.OrderByDescending(p => p.Status).ThenByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.Status).ThenByDescending(p => p.CreatedAt),

            "method" => desc
                ? query.OrderByDescending(p => p.Method).ThenByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.Method).ThenByDescending(p => p.CreatedAt),

            _ => desc
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt)
        };
    }

    private static PaymentResponseDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Currency = p.Currency,
        Status = p.Status.ToString(),
        Method = p.Method.ToString(),
        Description = p.Description,
        ExternalReference = p.ExternalReference,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
