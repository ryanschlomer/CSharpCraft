// File: Hubs/GameHub.cs
using CSharpCraft.Clases;

using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

public class GameHub : Hub
{

 
    ChunkService chunkService { get; set; }
    private PlayerManager _playerManager;
    private readonly Physics _physics;

    public GameHub(ChunkService chunkService, PlayerManager playerManager, Physics physics)
    {
        this.chunkService = chunkService;
        _playerManager = playerManager;
        _physics = physics;

    }
    public async Task<string> GetConnectionId()
    {
        return Context.ConnectionId;
    }
    //public async Task UpdatePlayerPosition(string playerId, float x, float y, float z, float rotation)
    //{
    //    // Broadcast updated position to other clients
    //    await Clients.Others.SendAsync("ReceivePosition", playerId, x, y, z, rotation);
    //}

    public async Task UpdatePlayerChunkData(ChunkData chunkData)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new Vector3JsonConverter(), new Vector2JsonConverter() }
        };

        var jsonChunkData = JsonSerializer.Serialize(chunkData, options);
        await Clients.Caller.SendAsync("ReceiveChunkData", jsonChunkData);
    }



    public override async Task OnConnectedAsync()
    {
        var player = new Player { ConnectionId = Context.ConnectionId };
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _playerManager.RemovePlayer(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Position> UpdatePlayer(float deltaTime, float xDelta, float yDelta, float zDelta, float dirX, float dirY, float dirZ)
    {
        var player = _playerManager.GetPlayer(Context.ConnectionId);
        if (player != null)
        {
            Vector3 movementDelta = new Vector3(xDelta, yDelta, zDelta);
            if(movementDelta != Vector3.Zero)
            {
                Console.WriteLine("mevementDelta: " +  movementDelta);
            }
            Vector3 lookDirection = new Vector3(dirX, dirY, dirZ);
            //movementDelta = new Vector3(xDelta, 1, zDelta);
            // Update the player's intended direction and position
            _physics.HandlePlayerInput(player, movementDelta, lookDirection, deltaTime);

            GetChunksToLoad(player);

            // After physics updates
            return player.CameraPosition;
        }
        return null;
    }

    public async Task GetChunksToLoad(Player player)
    {

        //Did player move to a new chunk
        if (VoxelData.DidCameraMoveToNewChunk(player.PreviousPosition, player.Position))
        {//Camera moved to another chunk
            (int, int) currentChunkCoords = (
                  VoxelData.GetChunkCoordinate(player.Position.X),
                  VoxelData.GetChunkCoordinate(player.Position.Z));

            //How to send this message to the UI
            //logMessage = "Camera in Chunk " + currentChunkCoords.Item1 + ", " + currentChunkCoords.Item2;



            await SendChunksToRenderer(player);
          


            //unload out of range chunks


            var chunksToRemove = "";
            
            await Clients.All.SendAsync("ChunksToRemove", player.ChunkManager.ChunksToUnload);

            //Update previous chunk coord

            player.PreviousChunkCoords = currentChunkCoords;
        }


        //Update previous position
        player.PreviousPosition = player.Position; //might not be needed. 
    }

    //This method should go to Game Hub amybe
    public async Task SendChunksToRenderer(Player player)
    {
    
        //load new chunks
        await player.ChunkManager.LoadAndProcessChunksAroundAsync();
    }

    public async Task SendChunkData(string jsonChunkData)
    {
        await Clients.All.SendAsync("ReceiveChunkData", jsonChunkData);
    }

    private Player GetPlayerByConnectionId(string connectionId)
    {
        Player player = _playerManager.GetPlayer(connectionId);
        if (player != null)
            return player;

        return null;
    }
}

