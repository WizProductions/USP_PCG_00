using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;
using Room = UnityEngine.RectInt;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]
    public class SimpleRoomPlacement : ProceduralGenerationMethod
    {
        [Header("Room Parameters")] [SerializeField]
        private int _maxRooms = 10;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            List<Room> Rooms = new List<Room>();
            int attempts = 0;

            for (int i = 0; i < _maxSteps; i++)
            {
                ++attempts;
                
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // <-- Room -->
                if (Rooms.Count >= _maxRooms) break;
                //Room placed?
                if (TryToPlaceARoom(out Room room))
                {
                    Rooms.Add(room);
                    Debug.Log($"Room placed at ({room.x}, {room.y})");
                }
                
                // Waiting between steps to see the result.
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }
            
            if (Rooms.Count < _maxRooms)
            {
                Debug.LogWarning($"RoomPlacer Only placed {Rooms.Count}/{_maxRooms} rooms after {attempts} attempts.");
            }
            
            if (Rooms.Count < 2)
            {
                Debug.Log("Not enough rooms to connect.");
                return;
            }
            
            // CORRIDOR CREATIONS
            for (int i = 0; i < Rooms.Count - 1; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                Vector2Int start = Rooms[i].GetCenter();
                Vector2Int end = Rooms[i + 1].GetCenter();
                
                CreateDogLegCorridor(start, end);
                
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken : cancellationToken);
            }

            // Final ground building.
            BuildGround();
        }

        private bool TryToPlaceARoom(out Room room)
        {
            room = new Room(
                RandomService.Range(0, Grid.Width),
                RandomService.Range(0, Grid.Lenght),
                RandomService.Range(3, 8),
                RandomService.Range(3, 8
                ));
            
            if (!CanPlaceRoom(room, 1)) return false;
            
            for (int j = 0; j < room.height; j++)
            {
                for (int k = 0; k < room.width; k++)
                {
                    if (Grid.TryGetCellByCoordinates(room.x + k, room.y + j, out var chosenCell))
                    {
                        AddTileToCell(chosenCell, ROOM_TILE_NAME, true);
                    }
                }
            }

            return true;
        }

        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");

            // Instantiate ground blocks
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int z = 0; z < Grid.Lenght; z++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                    {
                        Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                        continue;
                    }

                    AddTileToCell(chosenCell, GRASS_TILE_NAME, false);
                }
            }
        }
        
        // -------------------------------------- CORRIDOR --------------------------------------------- 
        
        /// Creates an L-shaped corridor between two points, randomly choosing horizontal-first or vertical-first
        private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
        {
            bool horizontalFirst = RandomService.Chance(0.5f);
            
            if (horizontalFirst)
            {
                // Draw horizontal line first, then vertical
                CreateHorizontalCorridor(start.x, end.x, start.y);
                CreateVerticalCorridor(start.y, end.y, end.x);
            }
            else
            {
                // Draw vertical line first, then horizontal
                CreateVerticalCorridor(start.y, end.y, start.x);
                CreateHorizontalCorridor(start.x, end.x, end.y);
            }
        }
        
        /// Creates a horizontal corridor from x1 to x2 at the given y coordinate
        private void CreateHorizontalCorridor(int x1, int x2, int y)
        {
            int xMin = Mathf.Min(x1, x2);
            int xMax = Mathf.Max(x1, x2);
            
            for (int x = xMin; x <= xMax; x++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;
                
                AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
            }
        }
        
        /// Creates a vertical corridor from y1 to y2 at the given x coordinate
        private void CreateVerticalCorridor(int y1, int y2, int x)
        {
            int yMin = Mathf.Min(y1, y2);
            int yMax = Mathf.Max(y1, y2);
            
            for (int y = yMin; y <= yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;
                
                AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
            }
        }
    }
}