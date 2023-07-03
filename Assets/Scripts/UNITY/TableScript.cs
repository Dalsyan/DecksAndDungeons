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
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                var cell = Instantiate(CellPrefab, Vector3.zero, Quaternion.identity);
                cell.transform.SetParent(Table.transform, false);
                cell.name = string.Format("({0}, {1})", row, column);
            }
        }
    }
}
