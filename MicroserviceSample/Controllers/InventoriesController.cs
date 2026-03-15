using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;

namespace MicroserviceSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoriesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var inventories = await db.Inventories
            .Include(i => i.Product)
            .ToListAsync();
        return Ok(inventories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var inventory = await db.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
        return inventory is null ? NotFound() : Ok(inventory);
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var inventory = await db.Inventories
            .Include(i => i.Product)
            .Where(i => i.ProductId == productId)
            .ToListAsync();
        return Ok(inventory);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InventoryRequest request)
    {
        var inventory = new Inventory
        {
            ProductId = request.ProductId,
            Sku = request.Sku,
            Quantity = request.Quantity
        };
        db.Inventories.Add(inventory);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = inventory.Id }, inventory);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] InventoryRequest request)
    {
        var inventory = await db.Inventories.FindAsync(id);
        if (inventory is null) return NotFound();

        inventory.ProductId = request.ProductId;
        inventory.Sku = request.Sku;
        inventory.Quantity = request.Quantity;
        await db.SaveChangesAsync();
        return Ok(inventory);
    }

    [HttpPatch("{id}/quantity")]
    public async Task<IActionResult> AdjustQuantity(int id, [FromBody] AdjustQuantityRequest request)
    {
        var inventory = await db.Inventories.FindAsync(id);
        if (inventory is null) return NotFound();

        inventory.Quantity += request.Adjustment;
        if (inventory.Quantity < 0) inventory.Quantity = 0;
        await db.SaveChangesAsync();
        return Ok(new { inventory.Id, inventory.Quantity });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var inventory = await db.Inventories.FindAsync(id);
        if (inventory is null) return NotFound();

        db.Inventories.Remove(inventory);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record InventoryRequest(int ProductId, string Sku, int Quantity);
public record AdjustQuantityRequest(int Adjustment);
