using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;

namespace CSharpCraft.Clases
{
    public class PlayerManager
    {
        private readonly Dictionary<string, Player> _players = new Dictionary<string, Player>();
        private readonly ChunkService _chunkService;
        private readonly IHubContext<GameHub> _gameHubContext;

        public PlayerManager(ChunkService chunkService, IHubContext<GameHub> gameHubContext)
        {
            _chunkService = chunkService;
            _gameHubContext = gameHubContext;
        }
        public Player AddPlayer(string connectionId)
        {
            Player player = new Player
            {
                ConnectionId = connectionId
            };
            player.ChunkManager = new ChunkManager(player, _chunkService, _gameHubContext);

            _players[connectionId] = player;
            return player;
        }

        public void RemovePlayer(string connectionId)
        {
            _players.Remove(connectionId);
        }

        public Player GetPlayer(string connectionId)
        {
            //Console.WriteLine($"Attempting to get player with connection ID: {connectionId}");

            _players.TryGetValue(connectionId, out Player player);
            return player;
        }

        public List<Player> GetAllPlayers()
        {
            List<Player> list = new List<Player>();
            foreach (Player p in _players.Values)
            {
                list.Add(p);
            }
            return list;
        }
    }

    public class InteropHelper
    {
        private IJSRuntime _jsRuntime;
        private PlayerManager _playerManager;

        public InteropHelper(IJSRuntime jsRuntime, PlayerManager playerManager)
        {
            _jsRuntime = jsRuntime;
            _playerManager = playerManager;
        }

        public async Task<Player> InitializePlayerFromJS()
        {
            var connectionId = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "connectionId");

            Player player = _playerManager.GetPlayer(connectionId);
            if (player == null)
            {
                player = _playerManager.AddPlayer(connectionId);
                //Console.WriteLine($"Player Added: {player.ConnectionId}");

                //Console.WriteLine("All Player Ids:");
                foreach (Player p in _playerManager.GetAllPlayers())
                {
                    Console.WriteLine(p.ConnectionId);
                }

            }
            return player;
        }

    }
}
