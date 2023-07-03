using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableScript : MonoBehaviour
{
    public GameObject Table;
    public GameObject CellPrefab;
    public int Rows = 6;
    public int Columns = 6;

    private void Start()
    {
        FillTableWithCells();
    }

    private void FillTableWithCells()
    {
        // Get the size of the cell prefab
        RectTransform cellRectTransform = CellPrefab.GetComponent<RectTransform>();
        Vector2 cellSize = cellRectTransform.rect.size;

        // Calculate the total size of the grid based on the cell size and number of rows/columns
        float gridWidth = cellSize.x * Columns;
        float gridHeight = cellSize.y * Rows;

        // Calculate the offset to position the grid at the top-left corner of the table
        float offsetX = -gridWidth / 2f + cellSize.x / 2f;
        float offsetY = gridHeight / 2f - cellSize.y / 2f;

        // Loop through each row and column to instantiate the cell prefab
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                // Calculate the position of the cell based on its row and column
                float posX = offsetX + column * cellSize.x;
                float posY = offsetY - row * cellSize.y;
                Vector3 cellPosition = new Vector3(posX, posY, 0f);

                // Instantiate the cell prefab at the calculated position
                var cell = Instantiate(CellPrefab, cellPosition, Quaternion.identity);
                cell.transform.SetParent(Table.transform, false);

                // Set the name of the cell based on its position
                cell.name = string.Format("({0}, {1})", row, column);
            }
        }
    }
}
