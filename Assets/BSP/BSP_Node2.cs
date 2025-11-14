using Components.ProceduralGeneration.BSP2;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.RandomService;

//#############################################################################
//##--------------------------------- CLASS ---------------------------------##
//#############################################################################

public class BSP_Node2
{

//#############################################################################
//##-------------------------------- FIELDS ---------------------------------##
//#############################################################################

    private RectInt Area = new();
    private bool bIsLeaf = false;
    private BSP_Node2[] Children = new BSP_Node2[2];
    private int Depth = 0;

//#############################################################################
//##-------------------------------- METHODS --------------------------------##
//#############################################################################

    public BSP_Node2(RectInt InArea)
    {
        Area = InArea;
        BSP2.Instance.Tree.Add(this);
        //Too small
        // if (Area.x <= BSP2.Instance.minRoomSize.x || Area.y <= BSP2.Instance.minRoomSize.y)
        // {
        //     bIsLeaf = true;
        //     return;
        // }
    }

    public async UniTask Split(int InDepth)
    {
        Depth = InDepth;
        BSP2 _BSP = BSP2.Instance;
        RandomService RS = _BSP.RandomService;

        bool isTooBig = Area.width > BSP2.Instance.maxRoomSize.x 
                        || Area.height > BSP2.Instance.maxRoomSize.y;
    
        bool isTooSmall = Area.width <= BSP2.Instance.minRoomSize.x * 2
                          || Area.height <= BSP2.Instance.minRoomSize.y * 2;
        
        bool shouldStop = !isTooBig && (Depth >= BSP2.Instance.MaxLastDepth || isTooSmall);
        
        if (shouldStop)
        {
            SetSelfLeaf();
            return;
        }
        
        bool bSplitHorizontally = RS.Chance(0.5f);

        RectInt AreaA = new RectInt();
        RectInt AreaB = new RectInt();
        bool bSplitFound = false;
        
        for (int i = 0; i < _BSP.SplitAttempts; ++i)
        {
            if (bSplitHorizontally)
            {
                if (!CanSplitHorizontally(RS, out int CutPosY)) 
                    continue;
                
                //CanSplit, fill Areas
                AreaA.x = this.Area.x;
                AreaA.y = this.Area.y;
                AreaA.width = this.Area.width;
                AreaA.height = CutPosY - this.Area.y;

                AreaB.x = this.Area.x;
                AreaB.y = CutPosY;
                AreaB.width = this.Area.width;
                AreaB.height = this.Area.y + this.Area.height - CutPosY;

                bSplitFound = true;
                break;
            }
            else
            {
               if (!CanSplitVertically(RS, out int CutPosX))
                continue;
               
               //CanSplit, fill Areas
               AreaA.x = this.Area.x;
               AreaA.y = this.Area.y;
               AreaA.width = CutPosX - this.Area.x;
               AreaA.height = this.Area.height;

               AreaB.x = CutPosX;
               AreaB.y = this.Area.y;
               AreaB.width = this.Area.x + this.Area.width - CutPosX;
               AreaB.height = this.Area.height;

               bSplitFound = true;
               break;
            }
        }

        if (!bSplitFound)
        {
            SetSelfLeaf();
            return;
        }
        
        TraceDebug(Color.crimson);

        BSP_Node2 NewNodeA = new BSP_Node2(AreaA);
        BSP_Node2 NewNodeB = new BSP_Node2(AreaB);
        Children[0] = NewNodeA;
        Children[1] = NewNodeB;
        
        await UniTask.Delay(BSP2.Instance.GridGenerator.StepDelay);
        
        await NewNodeA.Split(InDepth + 1);
        await NewNodeB.Split(InDepth + 1);
    }

    // private bool CanSplitHorizontally(RandomService RS, out int CutPosY)
    // {
    //     int minCut = Area.y + BSP2.Instance.minRoomSize.y;
    //     int maxCut = Area.y + Area.height - BSP2.Instance.minRoomSize.y;
    //     
    //     int maxFromStart = Area.y + BSP2.Instance.maxRoomSize.y;
    //     int minFromEnd = Area.y + Area.height - BSP2.Instance.maxRoomSize.y;
    //     
    //     if (maxFromStart < maxCut) maxCut = maxFromStart;
    //     if (minFromEnd > minCut) minCut = minFromEnd;
    //
    //     if (maxCut <= minCut)
    //     {
    //         CutPosY = 0;
    //         return false;
    //     }
    //
    //     CutPosY = RS.Range(minCut, maxCut);
    //     return true;
    // }
    //
    // private bool CanSplitVertically(RandomService RS, out int CutPosX)
    // {
    //     int minCut = Area.x + BSP2.Instance.minRoomSize.x;
    //     int maxCut = Area.x + Area.width - BSP2.Instance.minRoomSize.x;
    //     
    //     int maxFromStart = Area.x + BSP2.Instance.maxRoomSize.x;
    //     int minFromEnd = Area.x + Area.width - BSP2.Instance.maxRoomSize.x;
    //     
    //     if (maxFromStart < maxCut) maxCut = maxFromStart;
    //     if (minFromEnd > minCut) minCut = minFromEnd;
    //
    //     if (maxCut <= minCut)
    //     {
    //         CutPosX = 0;
    //         return false;
    //     }
    //
    //     CutPosX = RS.Range(minCut, maxCut);
    //     return true;
    // }
    
    private bool CanSplitHorizontally(RandomService RS, out int CutPosY)
    {
        // Génère un ratio aléatoire entre SplitRatio.x et SplitRatio.y
        float splitRatio = RS.Range(BSP2.Instance.SplitRatio.x, BSP2.Instance.SplitRatio.y);
        int splitHeight = Mathf.RoundToInt(Area.height * splitRatio);

        // Zone A : du bas jusqu'à la coupe
        int areaAHeight = splitHeight;
        // Zone B : de la coupe jusqu'en haut
        int areaBHeight = Area.height - splitHeight;

        // Vérifie seulement minRoomSize (pas maxRoomSize)
        if (areaAHeight < BSP2.Instance.minRoomSize.y || areaBHeight < BSP2.Instance.minRoomSize.y)
        {
            CutPosY = 0;
            return false;
        }

        CutPosY = Area.y + splitHeight;
        return true;
    }

    private bool CanSplitVertically(RandomService RS, out int CutPosX)
    {
        float splitRatio = RS.Range(BSP2.Instance.SplitRatio.x, BSP2.Instance.SplitRatio.y);
        int splitWidth = Mathf.RoundToInt(Area.width * splitRatio);

        int areaAWidth = splitWidth;
        int areaBWidth = Area.width - splitWidth;

        if (areaAWidth < BSP2.Instance.minRoomSize.x || areaBWidth < BSP2.Instance.minRoomSize.x)
        {
            CutPosX = 0;
            return false;
        }

        CutPosX = Area.x + splitWidth;
        return true;
    }

    private void SetSelfLeaf()
    {
        bIsLeaf = true;
        TraceDebug(Color.greenYellow);
    }

    private void PlaceRoom()
    {
        
    }

    private void TraceDebug(Color color)
    {
        BSP2.Instance.DebugDrawRect(Area, color, 3600f);
    }

}