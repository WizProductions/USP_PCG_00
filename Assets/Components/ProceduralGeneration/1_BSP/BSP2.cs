using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Room = UnityEngine.RectInt;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/BSP")]
    public class BSP2 : ProceduralGenerationMethod
    {
        [Header("Room Parameters")]
        [SerializeField] private int LeafCutCount = 3;
        [SerializeField] public bool bDebugLeaf = false;
        [SerializeField] public Vector2Int minRoomSize = new Vector2Int(3, 3);
        [SerializeField] public Vector2Int maxRoomSize = new Vector2Int(5, 5);
        [SerializeField] public int SplitAttempts = 5;

        public static List<GameObject> _debugObjects = new List<GameObject>();
        public static BSP2 Instance = null;

        public BSP2()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                if (Instance != this)
                {
                    Destroy(this);
                }
            }
        }

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            RectInt GridArea = new RectInt(
                (int)Grid.OriginPosition.x,
                (int)Grid.OriginPosition.z,
                Grid.Width,
                Grid.Lenght);
            
            var RootNode = new BSP_Node2(GridArea);
            
            // Final ground building.
            BuildGround();
        }

        private void DebugDrawRect(RectInt area, Color color, float duration)
        {
            Vector3 p1 = new Vector3(area.xMin, 0, area.yMin);
            Vector3 p2 = new Vector3(area.xMax, 0, area.yMin);
            Vector3 p3 = new Vector3(area.xMax, 0, area.yMax);
            Vector3 p4 = new Vector3(area.xMin, 0, area.yMax);
            Debug.DrawLine(p1, p2, color, duration);
            Debug.DrawLine(p2, p3, color, duration);
            Debug.DrawLine(p3, p4, color, duration);
            Debug.DrawLine(p4, p1, color, duration);
        }

        // -------------------------------------- CORRIDOR --------------------------------------------- 

        private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
        {
            bool horizontalFirst = RandomService.Chance(0.5f);

            if (horizontalFirst)
            {
                CreateHorizontalCorridor(start.x, end.x, start.y);
                CreateVerticalCorridor(start.y, end.y, end.x);
            }
            else
            {
                CreateVerticalCorridor(start.y, end.y, start.x);
                CreateHorizontalCorridor(start.x, end.x, end.y);
            }
        }

        private void CreateHorizontalCorridor(int x1, int x2, int y)
        {
            int xMin = Mathf.Min(x1, x2);
            int xMax = Mathf.Max(x1, x2);

            for (int x = xMin; x <= xMax; x++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                AddTileToCell(cell, CORRIDOR_TILE_NAME, false);
            }
        }

        private void CreateVerticalCorridor(int y1, int y2, int x)
        {
            int yMin = Mathf.Min(y1, y2);
            int yMax = Mathf.Max(y1, y2);

            for (int y = yMin; y <= yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                AddTileToCell(cell, CORRIDOR_TILE_NAME, false);
            }
        }
        
        private void BuildGround()
        {
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
    }
}

