using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;

namespace Components.ProceduralGeneration.CellularAutomata
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/CellularAutomata")]
    public class CellularAutomata : ProceduralGenerationMethod
    {
        [Header("Parameters")] 
        [SerializeField] private float noiseDensity = 50f;
        [SerializeField] private int MinCellsForPropagation = 4;
        [SerializeField] private string PropagationType = GRASS_TILE_NAME;
        [SerializeField] private string FallbackType = WATER_TILE_NAME;
        [SerializeField] private bool BorderCellsCanChange = false;
        [SerializeField] private int ProbabilityToBorderChange = 50;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            float noiseDensityToProb = noiseDensity / 100f;
            Cell[,] CellsGrid = new Cell[Grid.Lenght, Grid.Width];
            List<(Cell, string)> CellsTransitionList = new();
            
            //Step 1 -> Fill the grid by noiseDensity
            for (int x = (int)Grid.OriginPosition.x; x < Grid.Width; ++x) //Only for a positive grid
            {
                for (int y = (int)Grid.OriginPosition.y; y < Grid.Lenght; ++y) //Only for a positive grid
                {
                    bool bIsGrass = RandomService.Chance(noiseDensityToProb);
                    //Cell is added successfully
                    if (AddCell(x, y, bIsGrass, out Cell cellAdded))
                    {
                        CellsGrid[x, y] = cellAdded;
                    }
                }
            }
            
            //Step 2 -> Check Neighbors and change type if needed
            for (int i = 0; i < _maxSteps; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                for (int x = 0; x < Grid.Lenght; ++x)
                {
                    for (int y = 0; y < Grid.Lenght; ++y)
                    {
                        Cell cell = CellsGrid[x, y];
                        if (!IsBorderCellPos(x, y))
                        {
                            (Cell, string) newTransitionInfos;
                            newTransitionInfos.Item1 = cell;
                            
                            if (GetAroundCellsOfType(x, y, PropagationType) >= MinCellsForPropagation)
                            {
                                //new type is propagation type
                                newTransitionInfos.Item2 = PropagationType;
                            }
                            else
                            {
                                newTransitionInfos.Item2 = FallbackType;
                            }
                            
                            CellsTransitionList.Add(newTransitionInfos);
                        }
                        //BorderCell
                        else if (BorderCellsCanChange)
                        {
                            //Random choose if the border cell change
                            if (!RandomService.Chance(ProbabilityToBorderChange / 100)) continue;
                            
                            Debug.Log($"BorderCell: {x}, {y}");
                            
                            //Change to the propagation type
                            (Cell, string) newTransitionInfos;
                            newTransitionInfos.Item1 = cell;
                            newTransitionInfos.Item2 = FallbackType;
                                
                            CellsTransitionList.Add(newTransitionInfos);
                        }
                    }
                }
                
                //Transit current grid
                foreach (var TransitionInfos in CellsTransitionList)
                {
                    AddTileToCell(TransitionInfos.Item1, TransitionInfos.Item2, true);
                }
                CellsTransitionList.Clear();
            
                // Waiting between steps to see the result.
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }
        }

        private bool AddCell(int PosX, int PosY, bool bIsGrass, out Cell cellAdded)
        {
            if (!Grid.TryGetCellByCoordinates(PosX, PosY, out Cell foundCell))
            {
                Debug.LogError($"Unable to get cell on coordinates : ({PosX}, {PosY})");
                cellAdded = null;
                return false;
            }
            
            AddTileToCell(foundCell, bIsGrass ? GRASS_TILE_NAME : WATER_TILE_NAME, false);
            cellAdded = foundCell;
            return true;
        }

        private int GetAroundCellsOfType(int PosX, int PosY, string type)
        {
            int FoundCellsCount = 0;
            Vector2Int[] AroundCellsPos = new Vector2Int[8]
            {
                new Vector2Int(PosX + 1, PosY),
                new Vector2Int(PosX + 1, PosY - 1),
                new Vector2Int(PosX, PosY - 1),
                new Vector2Int(PosX - 1, PosY - 1),
                new Vector2Int(PosX - 1, PosY),
                new Vector2Int(PosX - 1, PosY + 1),
                new Vector2Int(PosX, PosY + 1),
                new Vector2Int(PosX + 1, PosY + 1)
            };

            foreach (var CellPos in AroundCellsPos)
            {
                if (Grid.TryGetCellByCoordinates(CellPos, out Cell FoundCell))
                {
                    if (FoundCell.GridObject.Template.Name == type) ++FoundCellsCount;
                }
            }
            return FoundCellsCount;
        }

        private bool IsBorderCellPos(int PosX, int PosY)
        {
            return PosX <= (int)Grid.OriginPosition.x 
                   || PosX >= Grid.Width - 1 
                   || PosY <= (int)Grid.OriginPosition.z 
                   || PosY >= Grid.Lenght - 1;
        }
    }
}