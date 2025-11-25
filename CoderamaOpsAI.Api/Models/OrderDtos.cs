using System.ComponentModel.DataAnnotations;
using CoderamaOpsAI.Dal.Entities;

namespace CoderamaOpsAI.Api.Models;

public class CreateOrderRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public OrderStatus Status { get; set; }
}

public class UpdateOrderRequest
{
    public int? UserId { get; set; }

    public int? ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }

    public OrderStatus? Status { get; set; }
}

public class OrderResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
