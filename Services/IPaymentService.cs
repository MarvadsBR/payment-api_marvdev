using PaymentApi.DTOs;

namespace PaymentApi.Services;

public interface IPaymentService
{
    Task<PagedResponseDto<PaymentResponseDto>> GetAllAsync(string? status, int page, int pageSize);
    Task<PaymentResponseDto?> GetByIdAsync(Guid id);
    Task<PaymentResponseDto> CreateAsync(CreatePaymentDto dto);
    Task<PaymentResponseDto?> UpdateStatusAsync(Guid id, UpdatePaymentStatusDto dto);

    /// <summary>
    /// Returns false if the payment was not found or its status is not Pending.
    /// </summary>
    Task<DeleteResult> DeleteAsync(Guid id);
}

public enum DeleteResult
{
    Success,
    NotFound,
    NotAllowed
}
