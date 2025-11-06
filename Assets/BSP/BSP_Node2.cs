using Components.ProceduralGeneration.SimpleRoomPlacement;
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
    private BSP_Node[] Children = new BSP_Node[2];

//#############################################################################
//##-------------------------------- METHODS --------------------------------##
//#############################################################################

    public BSP_Node2(RectInt InArea)
    {
        Area = InArea;
        //Too small
        // if (Area.x <= BSP2.Instance.minRoomSize.x || Area.y <= BSP2.Instance.minRoomSize.y)
        // {
        //     bIsLeaf = true;
        //     return;
        // }

        Split();
    }

    private void Split()
    {
        BSP2 _BSP = BSP2.Instance;
        RandomService RS = _BSP.RandomService;
        
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
                AreaA.height = CutPosY;

                AreaB.x = this.Area.x;
                AreaB.y = CutPosY;
                AreaB.width = this.Area.width;
                AreaB.height = this.Area.height;
            }
            else
            {
               if (!CanSplitVertically(RS, out int CutPosX))
                continue;
               
               //CanSplit, fill Areas
               AreaA.x = this.Area.x;
               AreaA.y = this.Area.y;
               AreaA.width = CutPosX;
               AreaA.height = this.Area.height;

               AreaB.x = CutPosX;
               AreaB.y = this.Area.y;
               AreaB.width = this.Area.width;
               AreaB.height = this.Area.height;
            }

            bSplitFound = true;
        }

        if (!bSplitFound) return;

        BSP_Node2 NewNodeA = new BSP_Node2(AreaA);
        BSP_Node2 NewNodeB = new BSP_Node2(AreaB);
    }

    private bool CanSplitHorizontally(RandomService RS, out int CutPosY)
    {
        CutPosY = RS.Range(Area.y + BSP2.Instance.minRoomSize.y, Area.y + BSP2.Instance.maxRoomSize.y);
        return false;
    }

    private bool CanSplitVertically(RandomService RS, out int CutPosX)
    {
        CutPosX = RS.Range(Area.x + BSP2.Instance.minRoomSize.x, Area.x + BSP2.Instance.maxRoomSize.x);
        return false;
    }

    private void PlaceRoom()
    {
        
    }

}