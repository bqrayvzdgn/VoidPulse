using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VoidPulse.Api.Hubs;

[Authorize]
public class TrafficHub : Hub
{
    private readonly ILogger<TrafficHub> _logger;

    public TrafficHub(ILogger<TrafficHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR client connected: {ConnectionId}, User: {User}",
            Context.ConnectionId, Context.User?.Identity?.Name ?? "anonymous");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR client disconnected: {ConnectionId}, Error: {Error}",
            Context.ConnectionId, exception?.Message ?? "none");
        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinTenant(string tenantId)
    {
        var groupName = $"tenant:{tenantId}";
        _logger.LogInformation("Client {ConnectionId} joining group: {Group}", Context.ConnectionId, groupName);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveTenant(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
    }

    public Task<object> Ping()
    {
        _logger.LogInformation("Ping received from {ConnectionId}", Context.ConnectionId);
        return Task.FromResult<object>(new { message = "pong", timestamp = DateTime.UtcNow, connectionId = Context.ConnectionId });
    }
}
