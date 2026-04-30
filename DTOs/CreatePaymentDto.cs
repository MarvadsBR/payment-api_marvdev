using System.ComponentModel.DataAnnotations;
using PaymentApi.Models;

namespace PaymentApi.DTOs;

public class CreatePaymentDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code (e.g. BRL, USD).")]
    public string Currency { get; set; } = "BRL";

    [Required]
    public PaymentMethod Method { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ExternalReference { get; set; }
}
