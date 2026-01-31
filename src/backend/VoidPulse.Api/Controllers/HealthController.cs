using Microsoft.AspNetCore.Mvc;
using VoidPulse.Application.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public HealthController(AppDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbHealthy = false;
        var cacheHealthy = false;

        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync();
        }
        catch { }

        try
        {
            await _cacheService.SetAsync("health:ping", "pong", TimeSpan.FromSeconds(5));
            var result = await _cacheService.GetAsync<string>("health:ping");
            cacheHealthy = result == "pong";
        }
        catch { }

        var status = dbHealthy && cacheHealthy ? "healthy"
            : dbHealthy ? "degraded"
            : "unhealthy";

        var statusCode = dbHealthy ? 200 : 503;

        return StatusCode(statusCode, new
        {
            status,
            timestamp = DateTime.UtcNow,
            checks = new
            {
                database = dbHealthy ? "connected" : "unavailable",
                cache = cacheHealthy ? "connected" : "unavailable"
            }
        });
    }
}
