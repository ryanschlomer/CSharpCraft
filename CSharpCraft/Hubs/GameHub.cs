// File: Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class GameHub : Hub
{
    public async Task UpdatePlayerPosition(string playerId, float x, float y, float z, float rotation)
    {
        // Broadcast updated position to other clients
        await Clients.Others.SendAsync("ReceivePosition", playerId, x, y, z, rotation);
    }
}
