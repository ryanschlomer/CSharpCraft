using System.Numerics;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace CSharpCraft.Clases
{

    public class ChunkManager
    {
        public ChunkService ChunkService { get; }
        public Player Player { get; }
        private readonly IHubContext<GameHub> _gameHubContext;
        public HashSet<string> ChunksToUnload { get; private set; } = new HashSet<string> ();
        public HashSet<string> newVisibleChunkIds { get; private set; } = new HashSet<string>();
        public HashSet<string> previousVisibleChunkIds { get; private set; } = new HashSet<string>();
        public Queue<ChunkData> renderQueue  = new Queue<ChunkData>();

        private readonly object renderQueueLock = new object();

        //public IHubContext<GameHub> _gameHubContext;

        // GameStateService gameStateService;

        //how will we handle chunks that need to unload and reload?
        public ChunkManager(Player player, ChunkService chunkService, IHubContext<GameHub> gameHubContext)
        {
            Player = player;
            ChunkService = chunkService;
            _gameHubContext = gameHubContext;
        }


        public async Task<Chunk> GetChunk(int x, int z)
        {
            return await ChunkService.GetOrCreateChunkAsync(x, z);
        }

        /// <summary>
        /// Gets all the chunk coords around the camera.
        /// This might need to change in the future to only get the chunks that are in front.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HashSet<(int, int)> GetChunksForCamera(Vector3 position)
        {
            HashSet<(int, int)> visibleChunks = new HashSet<(int, int)>();

            int chunkX = VoxelData.GetChunkCoordinate(position.X);
            int chunkZ = VoxelData.GetChunkCoordinate(position.Z);



            return visibleChunks;
        }
        //might need a list of the chunks that have their mesh set
        //and a flag if their mesh needs updating (whether by a block added or removed or a tree that grew.

        //need VisibleChunks list to track the chunks in the camera. 
        //FYI: Only VisibleChunks chunks can have blocks added or removed I think. min visible chunks is 2 radius

        /// <summary>
        /// Gets the chunks around the player and processes them async.
        /// 
        /// </summary>
        /// <param name="centerX"></param>
        /// <param name="centerZ"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public async Task LoadAndProcessChunksAroundAsync()
        {
            int radius = VoxelData.ChunkViewRadius; //This should be a player stat

            (int, int) currentChunkCoords = (
  VoxelData.GetChunkCoordinate(Player.Position.X),
  VoxelData.GetChunkCoordinate(Player.Position.Z));
            int chunkX = currentChunkCoords.Item1;
            int chunkZ = currentChunkCoords.Item2;
            //could change this up to first get a list of chunks that need loading
            //and add them to a queue on the server.
            //Then the client can keep processing. The server can check to see if the chunks are already in the queue.
            //Then the server can load/create the chunks and send them to the client one at a time
            newVisibleChunkIds.Clear();

            var loadingTasks = new List<Task>();
            for (int x = chunkX - radius; x <= chunkX + radius; x++)
            {
                for (int z = chunkZ - radius; z <= chunkZ + radius; z++)
                {
                    Task<Chunk> loadTask = ChunkService.GetOrCreateChunkAsync(x, z);
                    loadingTasks.Add(ProcessLoadedChunkAsync(loadTask)); // Add task to process the loaded chunk
                }
            }

            await Task.WhenAll(loadingTasks); // Wait for all loading tasks to complete

            ChunksToUnload = new HashSet<string>(previousVisibleChunkIds);
            //now remove the new ones
            foreach (string s in newVisibleChunkIds)
            {
                ChunksToUnload.Remove(s);
            }
            // Update visible chunk IDs after all chunks have been processed
            previousVisibleChunkIds = new HashSet<string>(newVisibleChunkIds);

            foreach (string s in ChunksToUnload)
            {
                Console.WriteLine($"ChunksToUnload chunks {s}");
            }
        }

        private async Task ProcessLoadedChunkAsync(Task<Chunk> loadTask)
        {
            try
            {
                Chunk chunk = await loadTask; // Await the completion of the loading task
                ProcessChunkMesh(chunk); // Process the mesh once the chunk is loaded
            }
            catch (Exception ex)
            {
                HandleChunkLoadFailure(ex); // Handle any exceptions that occur during loading
            }
        }


        int counter = 0;
        private void ProcessChunkMesh(Chunk chunk)
        {

            var chunkData = chunk.GetChunkData();
            if (!previousVisibleChunkIds.Contains(chunkData.ChunkId))
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new Vector3JsonConverter(), new Vector2JsonConverter() }
                };
                var jsonChunkData = JsonSerializer.Serialize(chunkData, options);

                _gameHubContext.Clients.All.SendAsync("ReceiveChunkData", jsonChunkData);
            }
            //keep track of the chunks we are sending to renderer. This list will be used to determine 
            //which chunks to to be unloaded.
            newVisibleChunkIds.Add(chunkData.ChunkId);
        }

        //private void ProcessChunkMesh(Chunk chunk)
        //{
        //    // Process the mesh data for the chunk


        //    lock (renderQueueLock)
        //    {
        //        var chunkData = chunk.GetChunkData();
        //        if (!previousVisibleChunkIds.Contains(chunkData.ChunkId))
        //        {
        //            renderQueue.Enqueue(chunkData);
        //        }
        //        //keep track of the chunks we are sending to renderer. This list will be used to determine 
        //        //which chunks to to be unloaded.
        //        newVisibleChunkIds.Add(chunkData.ChunkId);


        //    }
        //}

        public ChunkData DequeueChunkData()
        {
            lock (renderQueueLock)
            {
                return renderQueue.Count > 0 ? renderQueue.Dequeue() : null;
            }
        }

        public bool IsQueueEmpty()
        {
            if (renderQueue.Count == 0)
                return true;
            return false;
        }

        private void HandleChunkLoadFailure(Exception ex)
        {
            // Log the error or handle exceptions from chunk loading
        }

    }
}
