using UnityEngine;

namespace VTools.Grid
{
    public class BaseGridGenerator : MonoBehaviour
    {
        [Header("Grid Parameters")]
        [SerializeField] public int _gridXValue = 64;
        [SerializeField] public int _gridYValue = 64;
        [SerializeField] public float _cellSize = 1;
        [SerializeField] private Vector3 _startPosition = new(0, 0);
        
        public Grid Grid { get; private set; }
        
        protected virtual void Start()
        {
            GenerateGrid();
        }
        
        public virtual void GenerateGrid()
        {
            if (Grid != null)
            {
                ClearGrid();
            }

            Grid = new Grid(_gridXValue, _gridYValue, _cellSize, _startPosition, false);
        }
        
        public void AddGridObjectToCell(Cell cell, GridObjectTemplate template, bool overrideExistingObjects)
        {
            if (overrideExistingObjects && cell.ContainObject)
            {
                //Same template?
                if (cell.GridObject.Template == template) return;
                
                //Sprite object is not null => prefab is already instanced, just can switch sprite
                if (cell.View.SpriteObject)
                {
                    cell.View.SpriteObject.sprite = template.View.SpriteObject.sprite;
                    return;
                }
                
                cell.ClearGridObject();
            }

            if (cell.ContainObject && !overrideExistingObjects)
            {
                return;
            }

            if (cell.ContainObject && cell.GridObject.Template.Name == template.Name)
            {
                return;
            }
            
            GridObjectFactory.SpawnOnGridFrom(template, cell, Grid, transform);
        }

        public void ClearGrid()
        {
            foreach (var cell in Grid.Cells)
            {
                cell.ClearGridObject();
            }
            
            Grid = null;
        }
    }
}