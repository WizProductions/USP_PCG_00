using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Room = UnityEngine.RectInt;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/BSP")]
    public class BSP : ProceduralGenerationMethod
    {
        private int resursiveCheck;

        [Header("Room Parameters")]
        [SerializeField] private int LeafCutCount = 5;
        [SerializeField] private bool bDebugLeaf;
        [SerializeField] private Vector2Int minRoomSize = new Vector2Int(3, 3);

        public static List<GameObject> _debugObjects = new List<GameObject>();

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            var root = new BSP_Node();
            root.FillNode(null, new RectInt((int)Grid.OriginPosition.x, (int)Grid.OriginPosition.y, Grid.Width, Grid.Lenght), 0);
            await RecursiveCutLeaf(root, 1, cancellationToken);

            //Connects all rooms by root to children
            ConnectAllRooms(root);
            
            // Final ground building.
            BuildGround();
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

        private async UniTask RecursiveCutLeaf(BSP_Node node, int depth, CancellationToken cancellationToken)
        {
            // Stop splitting when reaching the max depth
            if (depth > LeafCutCount)
            {
                if (bDebugLeaf && node.MyArea.width >= minRoomSize.x && node.MyArea.height >= minRoomSize.y)
                {
                    DebugDrawRect(node.MyArea, Color.green, 10_000f);
                }
                // Try placing a room in this leaf
                TryCreateRoomInLeaf(node);
                return;
            }

            // If the area is already below minimal size on any axis, stop here
            if (node.MyArea.width < minRoomSize.x || node.MyArea.height < minRoomSize.y)
                return;

            // Determine which axes can be cut while respecting min sizes on both sides
            bool canCutHorizontally = node.MyArea.height >= (minRoomSize.y * 2);
            bool canCutVertically = node.MyArea.width >= (minRoomSize.x * 2);

            if (!canCutHorizontally && !canCutVertically)
            {
                // No valid cut possible; optionally visualize if big enough
                if (bDebugLeaf && node.MyArea.width >= minRoomSize.x && node.MyArea.height >= minRoomSize.y)
                {
                    DebugDrawRect(node.MyArea, Color.yellow, 10_000f);
                }
                TryCreateRoomInLeaf(node);
                return;
            }

            bool cutByX = canCutHorizontally && canCutVertically ? RandomService.Chance(0.5f) : canCutHorizontally;

            Debug.Log(++resursiveCheck);

            var childA = new BSP_Node { MyParent = node, MyDepth = depth + 1 };
            var childB = new BSP_Node { MyParent = node, MyDepth = depth + 1 };

            if (cutByX)
            {
                FillNodeByCutByX(node.MyArea, ref childA, ref childB);
            }
            else
            {
                FillNodeByCutByY(node.MyArea, ref childA, ref childB);
            }

            //if (childA.MyArea == node.MyArea || childB.MyArea == node.MyArea) return; // no cut possible

            node.MyChildren[0] = childA;
            node.MyChildren[1] = childB;

            if (bDebugLeaf)
            {
                if (childA.MyArea.width >= minRoomSize.x && childA.MyArea.height >= minRoomSize.y)
                    DebugDrawRect(childA.MyArea, Color.cyan, 10_000f);
                if (childB.MyArea.width >= minRoomSize.x && childB.MyArea.height >= minRoomSize.y)
                    DebugDrawRect(childB.MyArea, Color.cyan, 10_000f);
            }

            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

            await RecursiveCutLeaf(childA, depth + 1, cancellationToken);
            await RecursiveCutLeaf(childB, depth + 1, cancellationToken);
        }
        
        private void ConnectAllRooms(BSP_Node root)
        {
            ConnectAnchors(root);
        }
        
        private Vector2Int? ConnectAnchors(BSP_Node node)
        {
            if (node == null) return null;

            bool isLeaf = node.MyChildren == null
                          || (node.MyChildren.Length == 0)
                          || (node.MyChildren.Length == 2 && node.MyChildren[0] == null && node.MyChildren[1] == null);

            if (isLeaf)
                return node.bHasRoom ? CenterOf(node.MyArea) : (Vector2Int?)null;

            var left = node.MyChildren.Length > 0 ? node.MyChildren[0] : null;
            var right = node.MyChildren.Length > 1 ? node.MyChildren[1] : null;

            var a = ConnectAnchors(left);
            var b = ConnectAnchors(right);

            if (a.HasValue && b.HasValue && RandomService.Chance(0.5f))
                CreateDogLegCorridor(a.Value, b.Value);

            return a ?? b;
        }

        private Vector2Int CenterOf(RectInt r)
        {
            return new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);
        }

        private bool TryCreateRoomInLeaf(BSP_Node Node)
        {
            RectInt area = Node.MyArea;
            // Ensure minimal size
            if (area.width < minRoomSize.x || area.height < minRoomSize.y)
                return false;

            // Randomly size the room within the leaf, at least minRoomSize
            int maxWInclusive = area.width; // allow full width
            int maxHInclusive = area.height; // allow full height
            int roomW = RandomService.Range(minRoomSize.x, maxWInclusive + 1);
            int roomH = RandomService.Range(minRoomSize.y, maxHInclusive + 1);

            // Position range so that room fits in area
            int xStart = RandomService.Range(area.xMin, area.xMax - roomW + 1);
            int yStart = RandomService.Range(area.yMin, area.yMax - roomH + 1);

            var room = new Room(xStart, yStart, roomW, roomH);

            // If collision with existing rooms due to previous leaves, try a few times
            for (int attempt = 0; attempt < 50; attempt++)
            {
                if (CanPlaceRoom(room, 1))
                {
                    BuildRoom(room, ROOM_TILE_NAME);
                    Node.MyArea = room;
                    Node.bHasRoom = true;
                    return true;
                }
                // pick another position inside area
                xStart = RandomService.Range(area.xMin, area.xMax - roomW + 1);
                yStart = RandomService.Range(area.yMin, area.yMax - roomH + 1);
                room = new Room(xStart, yStart, roomW, roomH);
            }

            return false;
        }

        private void BuildRoom(RectInt room, string tileTemplate)
        {
            for (int j = 0; j < room.height; j++)
            {
                for (int k = 0; k < room.width; k++)
                {
                    if (Grid.TryGetCellByCoordinates(room.x + k, room.y + j, out var chosenCell))
                    {
                        AddTileToCell(chosenCell, tileTemplate, false);
                    }
                }
            }
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

        private void FillNodeByCutByX(RectInt nodeArea, ref BSP_Node nodeA, ref BSP_Node nodeB)
        {
            // Horizontal cut (split along X axis): choose Y cut inside bounds so both parts >= minRoomSize.y
            int minCut = nodeArea.yMin + minRoomSize.y;
            int maxCut = nodeArea.yMax - minRoomSize.y;
            // if (minCut >= maxCut)
            // {
            //     nodeA.MyArea = nodeArea;
            //     nodeB.MyArea = nodeArea;
            //     return;
            // }
            int cut = RandomService.Range(minCut, maxCut);

            if (bDebugLeaf)
            {
                Vector3 start = new Vector3(nodeArea.xMin, 0, cut);
                Vector3 end = new Vector3(nodeArea.xMax, 0, cut);
                Debug.DrawLine(start, end, Color.red, 10_000f);
            }

            RectInt areaA = new RectInt(nodeArea.xMin, nodeArea.yMin, nodeArea.width, cut - nodeArea.yMin);
            RectInt areaB = new RectInt(nodeArea.xMin, cut, nodeArea.width, nodeArea.yMax - cut);

            nodeA.MyArea = areaA;
            nodeB.MyArea = areaB;
        }

        private void FillNodeByCutByY(RectInt nodeArea, ref BSP_Node nodeA, ref BSP_Node nodeB)
        {
            // Vertical cut (split along Y axis): choose X cut inside bounds so both parts >= minRoomSize.x
            int minCut = nodeArea.xMin + minRoomSize.x;
            int maxCut = nodeArea.xMax - minRoomSize.x;
            // if (minCut >= maxCut)
            // {
            //     nodeA.MyArea = nodeArea;
            //     nodeB.MyArea = nodeArea;
            //     return;
            // }
            int cut = RandomService.Range(minCut, maxCut);

            if (bDebugLeaf)
            {
                Vector3 start = new Vector3(cut, 0, nodeArea.yMin);
                Vector3 end = new Vector3(cut, 0, nodeArea.yMax);
                Debug.DrawLine(start, end, new Color(1f, 0.5f, 0f), 10_000f);
            }

            RectInt areaA = new RectInt(nodeArea.xMin, nodeArea.yMin, cut - nodeArea.xMin, nodeArea.height);
            RectInt areaB = new RectInt(cut, nodeArea.yMin, nodeArea.xMax - cut, nodeArea.height);

            nodeA.MyArea = areaA;
            nodeB.MyArea = areaB;
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
    }
}

