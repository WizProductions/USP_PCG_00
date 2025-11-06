using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using VTools.Grid;

namespace Components.ProceduralGeneration.NoiseLibrary
{
    [Serializable]
    struct TileNoise
    {
        public TileNoise(float InNoiseDensity, ETileType inETileType)
        {
            NoiseDensity = InNoiseDensity;
            TileType = inETileType;
        }
        
        [SerializeField] [Range(0f, 1f)] public float NoiseDensity;
        public ETileType TileType;
    }
    
    [CreateAssetMenu(menuName = "Procedural Generation Method/NoiseLibraryTest")]
    public class NoiseLibrary : ProceduralGenerationMethod
    {
        [FormerlySerializedAs("TileNoiseArray")]
        [Header("Parameters")]
        [SerializeField] private List<TileNoise> TileNoiseList = new List<TileNoise>()
        {
            new(0f, ETileType.Water),
            new(0.25f, ETileType.Sand),
            new(0.45f, ETileType.Grass),
            new(0.9f, ETileType.Rock)
        };
        
        [DoNotSerialize] float[,] NoiseGrid;
        
        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            NoiseGrid = new float[Grid.Lenght, Grid.Width];
            
            //Step 1 -> Noise configuration
            FastNoiseLite FNL = new();
            FNL.SetSeed(RandomService.Seed);
            FNL.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            FNL.SetFrequency(0.01f);
            FNL.SetFractalType(FastNoiseLite.FractalType.PingPong);
            FNL.SetFractalOctaves(5);
            FNL.SetFractalLacunarity(2);
            FNL.SetFractalGain(0.65f);
            FNL.SetFractalWeightedStrength(-0.29f);
            FNL.SetFractalPingPongStrength(2.37f);
            FNL.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Hybrid);
            FNL.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
            FNL.SetCellularJitter(1.5f);
            FNL.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2Reduced);
            FNL.SetDomainWarpAmp(34f);

            //Fill noiseGrid
            for (int x = 0; x < Grid.Lenght; ++x)
            {
                for (int y = 0; y < Grid.Lenght; ++y)
                {
                    NoiseGrid[x, y] = GetNoiseData(FNL, x, y);
                }
            }
            
            //Step 2 -> Fill the grid by noise
            for (int i = 0; i < _maxSteps; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                for (int x = 0; x < Grid.Lenght; ++x)
                {
                    for (int y = 0; y < Grid.Lenght; ++y)
                    {
                        FillCellByNoise(x, y);
                    }
                }
                
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

        private void FillCellByNoise(int x, int y)
        {
            if (Grid.TryGetCellByCoordinates(x, y, out Cell foundCell))
            {
                float noise = NoiseGrid[x, y];
                TileNoise TL = GetTileTypeByNoise(noise);
                AddTileToCell(foundCell, TL.TileType.ToString(), true);
            }
        }

        protected virtual float GetNoiseData(FastNoiseLite FNL, int x, int y)
        {
            float noiseData = FNL.GetNoise(x, y);
            noiseData = (noiseData + 1) * 0.5f;
            return Math.Clamp(noiseData, -1, 1);
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

        private TileNoise GetTileTypeByNoise(float InNoise)
        {
            TileNoise found = new TileNoise();
            foreach (var TL in TileNoiseList)
            {
                if (TL.NoiseDensity <= InNoise)
                {
                    found = TL;
                }
                else if (TL.NoiseDensity > InNoise)
                {
                    return found;
                }
            }
            return found;
        }
    }
}