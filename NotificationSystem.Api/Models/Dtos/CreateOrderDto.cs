using System.ComponentModel.DataAnnotations;

public class CreateOrderDto
{
    [Required(ErrorMessage = "CustomerName is required")]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "TotalAmount must be a positive value")]
    public decimal TotalAmount { get; set; }
}