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
        [Header("Noise List")]
        [SerializeField] private List<TileNoise> TileNoiseList = new List<TileNoise>()
        {
            new(0f, ETileType.Water),
            new(0.15f, ETileType.Sand),
            new(0.25f, ETileType.Dirt),
            new(0.45f, ETileType.Grass),
            new(0.85f, ETileType.Rock)
        };
        
        [Header("Noise Parameters")]
        [SerializeField] private FastNoiseLite.NoiseType _NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        [SerializeField] private float _Frequency = 0.015f;
        [SerializeField] private FastNoiseLite.FractalType _FractalType = FastNoiseLite.FractalType.None;
        [SerializeField] private int _FractalOctaves = 0;
        [SerializeField] private float _FractalLacunarity = 0;
        [SerializeField] private float _FractalGain = 0f;
        [SerializeField] private float _FractalWeightedStrength = 0f;
        [SerializeField] private float _FractalPingPongStrength = 0f;
        [SerializeField] private FastNoiseLite.CellularDistanceFunction _CellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
        [SerializeField] private FastNoiseLite.CellularReturnType _CellularReturnType = FastNoiseLite.CellularReturnType.CellValue;
        [SerializeField] private float _CellularJitter = 1f;
        [SerializeField] private FastNoiseLite.DomainWarpType _DomainWarpType = FastNoiseLite.DomainWarpType.None;
        [SerializeField] private float _DomainWarpAmp = 0f;
        [SerializeField] private FastNoiseLite.RotationType3D _RotationType3D = FastNoiseLite.RotationType3D.None;
        
        [DoNotSerialize] float[,] NoiseGrid;
        
        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            NoiseGrid = new float[Grid.Lenght, Grid.Width];
            
            //Step 1 -> Noise configuration
            FastNoiseLite FNL = new();
            FNL.SetSeed(RandomService.Seed);
            FNL.SetNoiseType(_NoiseType);
            FNL.SetFrequency(_Frequency);
            FNL.SetFractalType(_FractalType);
            FNL.SetFractalOctaves(_FractalOctaves);
            FNL.SetFractalLacunarity(_FractalLacunarity);
            FNL.SetFractalGain(_FractalGain);
            FNL.SetFractalWeightedStrength(_FractalWeightedStrength);
            FNL.SetFractalPingPongStrength(_FractalPingPongStrength);
            FNL.SetCellularDistanceFunction(_CellularDistanceFunction);
            FNL.SetCellularReturnType(_CellularReturnType);
            FNL.SetCellularJitter(_CellularJitter);
            FNL.SetDomainWarpType(_DomainWarpType);
            FNL.SetDomainWarpAmp(_DomainWarpAmp);
            FNL.SetRotationType3D(_RotationType3D);
            

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