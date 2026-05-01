using System.ComponentModel.DataAnnotations;
using PaymentApi.Models;

namespace PaymentApi.DTOs;

public class UpdatePaymentStatusDto
{
    [Required]
    [EnumDataType(typeof(PaymentStatus), ErrorMessage = "Invalid status. Allowed values: Pending, Completed, Failed, Refunded.")]
    public PaymentStatus Status { get; set; }
}
