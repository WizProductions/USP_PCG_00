using System.Collections.Generic;
using System.Threading;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;

[CreateAssetMenu(menuName = "Procedural Generation Method/BSP_Correction")]
public class BSP_Correction : ProceduralGenerationMethod
{
    [Header("Split Parameters")]
    [Range(0,1)] public float HorizontalSplitChance = 0.5f;
    public Vector2 SplitRatio = new(0.3f, 0.7f);
    public int MaxSplitAttempt = 5;
    
    [Header("Leafs Parameters")]
    public Vector2Int LeafMinSize = new(8, 8);
    public Vector2Int RoomMaxSize = new(7, 7);
    public Vector2Int RoomMinSize = new(5, 5);
    
    [Header("Debug")]
    public List<Node> Tree;
    [SerializeField] public bool bDebugLeaf = false;
    [SerializeField] private bool bDebugDrawGrid = false;
    
    public static BSP_Correction Instance = null;

    public BSP_Correction()
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
        Tree = new List<Node>();
        if (bDebugDrawGrid) DebugDrawGrid();
        
        var allGrid = new RectInt(0, 0, Grid.Width, Grid.Lenght);
        // Create all tree.
        var root = new Node(RandomService, this, allGrid);
        Tree.Add(root);
        
        root.ConnectSisters();
    }
    
    private void DebugDrawGrid()
    {
        float yLevel = -1;
        Color color = new(.15f, .15f, .15f, .5f);
        float duration = 999999f;
            
        for (int x = 0; x <= Grid.Width; ++x)
        {
            Vector3 start = new Vector3(x + Grid.OriginPosition.x, yLevel, Grid.OriginPosition.z);
            Vector3 end = new Vector3(x + Grid.OriginPosition.x, yLevel, Grid.Lenght + Grid.OriginPosition.z);
            Debug.DrawLine(start, end, color, duration);
        }
            
        for (int y = 0; y <= Grid.Lenght; ++y)
        {
            Vector3 start = new Vector3(Grid.OriginPosition.x, yLevel, y + Grid.OriginPosition.z);
            Vector3 end = new Vector3(Grid.Width + Grid.OriginPosition.x, yLevel, y + Grid.OriginPosition.z);
            Debug.DrawLine(start, end, color, duration);
        }
    }
    
    public void DebugDrawRect(RectInt area, Color color, float duration, float yLevel = 0)
    {
        if (!bDebugLeaf) return;
            
        Vector3 p1 = new Vector3(area.xMin, yLevel, area.yMin);
        Vector3 p2 = new Vector3(area.xMax, yLevel, area.yMin);
        Vector3 p3 = new Vector3(area.xMax, yLevel, area.yMax);
        Vector3 p4 = new Vector3(area.xMin, yLevel, area.yMax);
        Debug.DrawLine(p1, p2, color, duration);
        Debug.DrawLine(p2, p3, color, duration);
        Debug.DrawLine(p3, p4, color, duration);
        Debug.DrawLine(p4, p1, color, duration);
    }
}

[System.Serializable]
public class Node
{
    [SerializeField] private RectInt _room;
    [SerializeField] private bool _isLeaf;
    private readonly RandomService _randomService;
    private readonly BaseGridGenerator _gridGenerator;
    private readonly BSP_Correction _bsp2;
    
    private Node _child1;
    private Node _child2;

    public Node(RandomService randomService, BSP_Correction bsp2, RectInt room)
    {
        _randomService = randomService;
        _bsp2 = bsp2;
        _gridGenerator = bsp2.GridGenerator;
        _room = room;
        
        BSP_Correction.Instance.DebugDrawRect(_room, Color.crimson, 999999);
        Split();
    }

    private void Split()
    {
        RectInt splitBoundsLeft = default;
        RectInt splitBoundsRight = default;
        bool splitFound = false;

        for (int i = 0; i < _bsp2.MaxSplitAttempt; i++)
        {
            bool horizontal = _randomService.Chance(_bsp2.HorizontalSplitChance);
            float splitRatio = _randomService.Range(_bsp2.SplitRatio.x, _bsp2.SplitRatio.y);
        
            if (horizontal)
            {
                if (!CanSplitHorizontally(splitRatio, out splitBoundsLeft, out splitBoundsRight))
                {
                    continue;
                }
            }
            else
            {
                if (!CanSplitVertically(splitRatio, out splitBoundsLeft, out splitBoundsRight))
                {
                    continue;
                }
            }
            
            splitFound = true;
            break;
        }

        // Stop recursion, it's a Leaf !
        if (!splitFound)
        {
            _isLeaf = true;
            PlaceRoom(_room);
            
            BSP_Correction.Instance.DebugDrawRect(_room, Color.greenYellow, 999999);
            
            return;
        }

        _child1 = new Node(_randomService, _bsp2, splitBoundsLeft);
        _child2 = new Node(_randomService, _bsp2, splitBoundsRight);
        
        _bsp2.Tree.Add(_child1);
        _bsp2.Tree.Add(_child2);
    }

    private bool CanSplitHorizontally(float splitRatio ,out RectInt firstSplit, out RectInt secondSplit)
    {
        int widthSplit = Mathf.RoundToInt(_room.width * splitRatio);

        var firstSplitWidth = widthSplit;
        var firstSplitHeight = _room.height;
        firstSplit = new RectInt(_room.xMin, _room.yMin, firstSplitWidth, firstSplitHeight);
        
        var secondSplitWidth = _room.width - widthSplit;
        var secondSplitHeight = _room.height;
        secondSplit = new RectInt(_room.xMin + widthSplit, _room.yMin, secondSplitWidth, secondSplitHeight);
        
        if (firstSplit.width < _bsp2.LeafMinSize.x || firstSplit.height < _bsp2.LeafMinSize.y)
        {
            return false;
        }

        if (secondSplit.width < _bsp2.LeafMinSize.x || secondSplit.height < _bsp2.LeafMinSize.y)
        {
            return false;
        }
        
        return true;
    }
    
    private bool CanSplitVertically(float splitRatio ,out RectInt firstSplit, out RectInt secondSplit)
    {
        int heightSplit = Mathf.RoundToInt(_room.height * splitRatio);

        var firstSplitWidth = _room.width;
        var firstSplitHeight = heightSplit;
        firstSplit = new RectInt(_room.xMin, _room.yMin, firstSplitWidth, firstSplitHeight);
        
        var secondSplitWidth = _room.width;
        var secondSplitHeight = _room.height - heightSplit;
        secondSplit = new RectInt(_room.xMin, _room.yMin + heightSplit, secondSplitWidth, secondSplitHeight);
        
        if (firstSplit.width < _bsp2.LeafMinSize.x || firstSplit.height < _bsp2.LeafMinSize.y)
        {
            return false;
        }

        if (secondSplit.width < _bsp2.LeafMinSize.x || secondSplit.height < _bsp2.LeafMinSize.y)
        {
            return false;
        }
        
        return true;
    }
    
    
    /// Marks the grid cells of the room as occupied
    private void PlaceRoom(RectInt room)
    {
        // Add some randomness to the room size.
        var newRoomLength = _randomService.Range(_bsp2.RoomMinSize.x, _bsp2.RoomMaxSize.x + 1);
        var newRoomWidth = _randomService.Range(_bsp2.RoomMinSize.y, _bsp2.RoomMaxSize.y + 1);
        
        room.width = newRoomWidth;
        room.height = newRoomLength;
        
        // Reinject into room to get the correct center.
        _room = room;
        
        for (int ix = room.xMin; ix < room.xMax; ix++)
        {
            for (int iy = room.yMin; iy < room.yMax; iy++)
            {
                if (!_gridGenerator.Grid.TryGetCellByCoordinates(ix, iy, out var cell)) 
                    continue;
                    
                var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Room");
                _gridGenerator.AddGridObjectToCell(cell, groundTemplate, true);
            }
        }
    }

    private Node GetLastChild()
    {
        if (_child1 != null)
        {
            return _child1.GetLastChild();
        }

        return this;
    }

    public void ConnectSisters()
    {
        // It's a leaf, nothing to do here.
        if (_child1 == null || _child2 == null) 
            return;
        
        // Connect sisters
        ConnectNodes(_child1, _child2);
            
        // Connect child of sisters
        _child1.ConnectSisters();
        _child2.ConnectSisters();
    }

    private void ConnectNodes(Node node1, Node node2)
    {
        var center1 = node1.GetLastChild()._room.GetCenter();
        var center2 = node2.GetLastChild()._room.GetCenter();
        
        CreateDogLegCorridor(center1, center2);
    }
    
    /// Creates an L-shaped corridor between two points, randomly choosing horizontal-first or vertical-first
    private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
    {
        bool horizontalFirst = _randomService.Chance(0.5f);

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
            if (!_gridGenerator.Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;
                
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Corridor");
            _gridGenerator.AddGridObjectToCell(cell, groundTemplate, false);
        }
    }
        
    /// Creates a vertical corridor from y1 to y2 at the given x coordinate
    private void CreateVerticalCorridor(int y1, int y2, int x)
    {
        int yMin = Mathf.Min(y1, y2);
        int yMax = Mathf.Max(y1, y2);
            
        for (int y = yMin; y <= yMax; y++)
        {
            if (!_gridGenerator.Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;
                
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Corridor");
            _gridGenerator.AddGridObjectToCell(cell, groundTemplate, false);
        }
    }
}