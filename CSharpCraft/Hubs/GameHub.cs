// File: Hubs/GameHub.cs
using CSharpCraft.Clases;
using CSharpCraft.Clases.Item;
using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpCraft.Clases.Item;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

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

    //public async Task<Position> UpdatePlayer(float deltaTime, float dirX, float dirY, float dirZ, keyStates)
    public async Task<Position> UpdatePlayer(PlayerUpdateData data)
    {
        var player = _playerManager.GetPlayer(Context.ConnectionId);
        if (player != null)
        {
            int xDelta = 0, yDelta = 0, zDelta = 0;
            if (data.KeyStates.TryGetValue("ArrowUp", out bool arrowUp) && arrowUp)
            {
                zDelta += 1;
            }
            if (data.KeyStates.TryGetValue("ArrowDown", out bool arrowDown) && arrowDown)
            {
                zDelta -= 1;
            }
            if (data.KeyStates.TryGetValue("ArrowLeft", out bool arrowLeft) && arrowLeft)
            {
                xDelta -= 1;
            }
            if (data.KeyStates.TryGetValue("ArrowRight", out bool arrowRight) && arrowRight)
            {
                xDelta += 1;
            }
            if (data.KeyStates.TryGetValue("Space", out bool space) && space) //Jump
            {
                yDelta += 1;
            }
            if (data.KeyStates.TryGetValue("Sneak", out bool sneak) && sneak)
            {//check sneak, run, and then walk
                player.CurrentSpeed = player.SneakSpeed;
            }
            else if (data.KeyStates.TryGetValue("Run", out bool run) && run)
            {
                player.CurrentSpeed = player.RunSpeed;
            }
            else
            {
                player.CurrentSpeed = player.Speed;
            }

            if (data.KeyStates.TryGetValue("Fly", out bool fly) && fly)
            {//flying isn't implemented. increase jump height for now
                player.CurrentJumpHeight = 2.2f;
            }
            else
            {
                player.CurrentJumpHeight = player.JumpHeight;
            }


            Vector3 movementDelta = new Vector3(xDelta, yDelta, zDelta);
            if (movementDelta != Vector3.Zero)
            {
                Console.WriteLine("mevementDelta: " + movementDelta);
            }
            Vector3 lookDirection = new Vector3(data.DirectionX, data.DirectionY, data.DirectionZ);
            //movementDelta = new Vector3(xDelta, 1, zDelta);
            // Update the player's intended direction and position
            _physics.HandlePlayerInput(player, movementDelta, lookDirection, data.DeltaTime);

            GetChunksToLoad(player);

            // After physics updates
            return player.CameraPosition;
        }
        return null;
    }

    public async Task GetChunksToLoad(Player player)
    {
        if (!VoxelData.DidCameraMoveToNewChunk(player.PreviousPosition, player.Position))
        {//only check to see if new chunks need to be generated to keep them generated before the player gets there
            //player.ChunkManager.GenerateChunksAround((int)player.Position.X, (int)player.Position.Y, VoxelData.ChunkViewRadius + 1);

        }
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

            await Clients.Caller.SendAsync("ChunksToRemove", player.ChunkManager.ChunksToUnload);

            //Update previous chunk coord

            player.PreviousChunkCoords = currentChunkCoords;
        }


        //Update previous position
        player.PreviousPosition = player.Position; //might not be needed. 
    }


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

    public async Task HandleBlockInteraction(float x, float y, float z, string code)
    {
        //REMOVE = RemoveBlock

        bool removeSuccess = false;
        bool addSuccess = false;
        bool playerInWay = false;
        if (code != "REMOVE")
        {
            byte blockType = byte.Parse(code); //get this based on the code sent

            BoundingBox block = new BoundingBox(new Vector3(x, y, z), new Vector3(x + 1, y + 1, z + 1));
            //Need to see if any players are in the block area
            foreach (Player p in _playerManager.GetAllPlayers())
            {
                if (p.BoundingCylinder.Intersects(block))
                {
                    playerInWay = true;
                    break;
                }
            }

            if (!playerInWay)
                addSuccess = await chunkService.AddBlock((int)x, (int)y, (int)z, (byte)blockType);

            if (addSuccess)
            {
                List<BlockUpdate> blocksToUpdate = new List<BlockUpdate>();
                blocksToUpdate.Add(new BlockUpdate
                {
                    X = (int)x,
                    Y = (int)y,
                    Z = (int)z,
                    BlockType = blockType
                });

                //Send block to all clients
                await Clients.All.SendAsync("UpdateBlocks", blocksToUpdate);
            }
        }
        else //send in blockType = 0 to delete
        {
            removeSuccess = await chunkService.RemoveBlock((int)x, (int)y, (int)z);

            if (removeSuccess)
            {
                // Define the region around the deleted block
                int regionSize = 1; // 1 block radius around the deleted block
                List<BlockUpdate> blocksToUpdate = new List<BlockUpdate>();

                for (int dx = -regionSize; dx <= regionSize; dx++)
                {
                    for (int dy = -regionSize; dy <= regionSize; dy++)
                    {
                        for (int dz = -regionSize; dz <= regionSize; dz++)
                        {
                            int blockX = (int)x + dx;
                            int blockY = (int)y + dy;
                            int blockZ = (int)z + dz;
                            byte currentBlockType = await chunkService.GetBlockType(blockX, blockY, blockZ);

                            if (currentBlockType == 0)
                            {//if it's an air block but not the one we are deleting.
                                //we need to send the deleted block back to the client
                                if (blockX != (int)x || blockY != (int)y || blockZ != (int)z)
                                    continue;
                            }

                            Vector2 chunkCoords = VoxelData.CalculateChunkPosition(x, z);
                            string chunkId = Chunk.GetChunkIdByCoords((int)chunkCoords.X, (int)chunkCoords.Y);

                            blocksToUpdate.Add(new BlockUpdate
                            {
                                X = blockX,
                                Y = blockY,
                                Z = blockZ,
                                BlockType = currentBlockType,
                                ChunkId = chunkId
                            }); ;
                        }
                    }
                }

                // Notify all clients about the block updates
                await Clients.All.SendAsync("UpdateBlocks", blocksToUpdate);
            }
        }
    }

    /// <summary>
    /// Gets the UV Data for all the blok types.
    /// Client will store this info.
    /// </summary>
    /// <returns>List<UVData></returns>
    public async Task<List<UVData>> GetUVData()
    {
        var uvData = VoxelData.CompileUVData();
        return uvData;
    }

    public async Task<Item> GetCurrentItem()
    {
        var player = _playerManager.GetPlayer(Context.ConnectionId);
        if (player != null)
        {
            return player.Hotbar.SelectedItem;
        }

        return null;
    }

    public async Task<Item> SetCurrentItemByKey(int key)
    {
        var player = _playerManager.GetPlayer(Context.ConnectionId);
        if (player != null)
        {
            player.Hotbar.SelectItemByKey(key);
            return player.Hotbar.SelectedItem;
        }

        return null;
    }

    public async Task<List<Item>> GetPlayerHotbarItems()
    {
        var player = _playerManager.GetPlayer(Context.ConnectionId);
        if (player != null)
        {
            return player.Hotbar.GetAllItems();
        }

        return null;
    }

    public class PlayerUpdateData
    {
        public float DeltaTime { get; set; }
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        public float DirectionZ { get; set; }
        public Dictionary<string, bool> KeyStates { get; set; }
    }
}
