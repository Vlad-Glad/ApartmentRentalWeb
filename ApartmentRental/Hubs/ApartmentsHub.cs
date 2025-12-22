using Microsoft.AspNetCore.SignalR;

namespace ApartmentRental.Hubs;

public sealed class ApartmentsHub : Hub
{
    // Optional: join a city group (for filtered live updates)
    public async Task JoinCity(string city)
    {
        if (!string.IsNullOrWhiteSpace(city))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"city:{city}");
    }

    public async Task LeaveCity(string city)
    {
        if (!string.IsNullOrWhiteSpace(city))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"city:{city}");
    }
}
