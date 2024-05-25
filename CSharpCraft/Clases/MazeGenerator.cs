namespace CSharpCraft.Clases
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public class MazeGenerator
    {
        private const int ChunkSize = 16;

        public byte[,,] GenerateMazeChunk(byte[,,] blocks, int mazeYLevel, byte wallBlockType, byte floorBlockType, byte airBlockType)
        {
            // Initialize the maze grid
            bool[,] maze = GenerateMaze(ChunkSize, ChunkSize);

            // Create the bottom layer
            CreateBottomLayer(blocks, mazeYLevel, floorBlockType);

            // Create the walls and paths
            CreateWallsAndPaths(blocks, maze, mazeYLevel, wallBlockType, airBlockType);

            // Create the top layer
            CreateTopLayer(blocks, mazeYLevel + 3, floorBlockType);

            return blocks;
        }

        private bool[,] GenerateMaze(int width, int height)
        {
            bool[,] maze = new bool[width, height];
            Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
            Random rand = new Random();

            int startX = rand.Next(width);
            int startY = rand.Next(height);
            stack.Push((startX, startY));
            maze[startX, startY] = true;

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                var neighbors = GetUnvisitedNeighbors(maze, x, y);

                if (neighbors.Count > 0)
                {
                    stack.Push((x, y));

                    var (nx, ny) = neighbors[rand.Next(neighbors.Count)];
                    maze[nx, ny] = true;

                    // Remove wall between current cell and neighbor
                    maze[(x + nx) / 2, (y + ny) / 2] = true;

                    stack.Push((nx, ny));
                }
            }

            return maze;
        }

        private List<(int, int)> GetUnvisitedNeighbors(bool[,] maze, int x, int y)
        {
            List<(int, int)> neighbors = new List<(int, int)>();
            int[] dx = { 2, -2, 0, 0 };
            int[] dy = { 0, 0, 2, -2 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && ny >= 0 && nx < maze.GetLength(0) && ny < maze.GetLength(1) && !maze[nx, ny])
                {
                    neighbors.Add((nx, ny));
                }
            }

            return neighbors;
        }

        private void CreateBottomLayer(byte[,,] blocks, int yLevel, byte blockType)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    blocks[x, yLevel, z] = blockType;
                }
            }
        }

        private void CreateWallsAndPaths(byte[,,] blocks, bool[,] maze, int yLevel, byte wallBlockType, byte airBlockType)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    if (maze[x, z])
                    {
                        // Path
                        blocks[x, yLevel + 1, z] = airBlockType;
                        blocks[x, yLevel + 2, z] = airBlockType;
                    }
                    else
                    {
                        // Wall
                        blocks[x, yLevel + 1, z] = wallBlockType;
                        blocks[x, yLevel + 2, z] = wallBlockType;
                    }
                }
            }
        }

        private void CreateTopLayer(byte[,,] blocks, int yLevel, byte blockType)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    blocks[x, yLevel, z] = blockType;
                }
            }
        }
    }

   


}
