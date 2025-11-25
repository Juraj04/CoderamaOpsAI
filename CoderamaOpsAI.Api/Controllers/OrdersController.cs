using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(AppDbContext dbContext, ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.Product)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = orders.Select(o => new OrderResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            UserName = o.User.Name,
            ProductId = o.ProductId,
            ProductName = o.Product.Name,
            Quantity = o.Quantity,
            Price = o.Price,
            Total = o.Total,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            return NotFound(new { message = "Order not found" });

        var response = new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserName = order.User.Name,
            ProductId = order.ProductId,
            ProductName = order.Product.Name,
            Quantity = order.Quantity,
            Price = order.Price,
            Total = order.Total,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Validate FK existence
        var userExists = await _dbContext.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
            return BadRequest(new { message = "User not found" });

        var productExists = await _dbContext.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
            return BadRequest(new { message = "Product not found" });

        // Calculate total server-side
        var total = request.Quantity * request.Price;

        var order = new Order
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Price = request.Price,
            Total = total,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Load navigation properties for response
        await _dbContext.Entry(order).Reference(o => o.User).LoadAsync(cancellationToken);
        await _dbContext.Entry(order).Reference(o => o.Product).LoadAsync(cancellationToken);

        var response = new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserName = order.User.Name,
            ProductId = order.ProductId,
            ProductName = order.Product.Name,
            Quantity = order.Quantity,
            Price = order.Price,
            Total = order.Total,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            return NotFound(new { message = "Order not found" });

        // Validate FKs if they are being changed
        if (request.UserId.HasValue)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Id == request.UserId.Value, cancellationToken);

            if (!userExists)
                return BadRequest(new { message = "User not found" });

            order.UserId = request.UserId.Value;
        }

        if (request.ProductId.HasValue)
        {
            var productExists = await _dbContext.Products
                .AnyAsync(p => p.Id == request.ProductId.Value, cancellationToken);

            if (!productExists)
                return BadRequest(new { message = "Product not found" });

            order.ProductId = request.ProductId.Value;
        }

        // Update other fields
        if (request.Quantity.HasValue)
            order.Quantity = request.Quantity.Value;

        if (request.Price.HasValue)
            order.Price = request.Price.Value;

        if (request.Status.HasValue)
            order.Status = request.Status.Value;

        // Recalculate total if quantity or price changed
        if (request.Quantity.HasValue || request.Price.HasValue)
        {
            order.Total = order.Quantity * order.Price;
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload navigation properties if FKs changed
        if (request.UserId.HasValue || request.ProductId.HasValue)
        {
            await _dbContext.Entry(order).Reference(o => o.User).LoadAsync(cancellationToken);
            await _dbContext.Entry(order).Reference(o => o.Product).LoadAsync(cancellationToken);
        }

        var response = new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserName = order.User.Name,
            ProductId = order.ProductId,
            ProductName = order.Product.Name,
            Quantity = order.Quantity,
            Price = order.Price,
            Total = order.Total,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.FindAsync(new object[] { id }, cancellationToken);

        if (order == null)
            return NotFound(new { message = "Order not found" });

        _dbContext.Orders.Remove(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
