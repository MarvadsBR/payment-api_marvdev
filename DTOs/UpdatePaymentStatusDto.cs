using System.ComponentModel.DataAnnotations;
using PaymentApi.Models;

namespace PaymentApi.DTOs;

public class UpdatePaymentStatusDto
{
    [Required]
    public PaymentStatus Status { get; set; }
}
