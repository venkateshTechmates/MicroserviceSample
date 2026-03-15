using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Infrastructure.Data;

namespace MicroserviceSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await db.Payments
            .Include(p => p.Order)
            .ToListAsync();
        return Ok(payments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var payments = await db.Payments
            .Where(p => p.OrderId == orderId)
            .ToListAsync();
        return Ok(payments);
    }
}
