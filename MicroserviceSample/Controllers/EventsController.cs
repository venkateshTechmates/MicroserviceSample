using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Sagas;

namespace MicroserviceSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(ApplicationDbContext db) : ControllerBase
{
    /// <summary>Get all stored events ordered by timestamp</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await db.StoredEvents
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Get all events for a specific correlationId (saga flow trace)</summary>
    [HttpGet("{correlationId}")]
    public async Task<IActionResult> GetByCorrelationId(Guid correlationId)
    {
        var events = await db.StoredEvents
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Get all distinct event types</summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetEventTypes()
    {
        var types = await db.StoredEvents
            .Select(e => e.EventType)
            .Distinct()
            .ToListAsync();
        return Ok(types);
    }

    /// <summary>Get all active and completed saga states</summary>
    [HttpGet("sagas")]
    public async Task<IActionResult> GetSagaStates()
    {
        var states = await db.Set<OrderSagaState>()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return Ok(states);
    }

    /// <summary>Get saga state by correlationId</summary>
    [HttpGet("sagas/{correlationId}")]
    public async Task<IActionResult> GetSagaByCorrelationId(Guid correlationId)
    {
        var state = await db.Set<OrderSagaState>()
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);
        return state is null ? NotFound() : Ok(state);
    }
}
