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
        // Get the size of the table
        Vector3 tableSize = Table.transform.localScale;

        // Calculate the size of each cell based on the table's size and number of rows/columns
        float cellWidth = tableSize.x / Columns;
        float cellHeight = tableSize.y / Rows;

        // Calculate the offset to position the cells at the top-left corner of the table
        float offsetX = -tableSize.x / 2f + cellWidth / 2f;
        float offsetY = tableSize.y / 2f - cellHeight / 2f;

        // Loop through each row and column to instantiate the cell prefab
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                // Calculate the position of the cell based on its row and column
                float posX = offsetX + column * cellWidth;
                float posY = offsetY - row * cellHeight;
                Vector3 cellPosition = new Vector3(posX, posY, 0f);

                // Instantiate the cell prefab at the calculated position
                GameObject cell = Instantiate(CellPrefab, Table.transform);
                cell.transform.localPosition = cellPosition;

                // Set the name of the cell based on its position
                cell.name = string.Format("({0}, {1})", row, column);
            }
        }
    }
}
