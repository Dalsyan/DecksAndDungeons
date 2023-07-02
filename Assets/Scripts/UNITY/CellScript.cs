using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellScript : MonoBehaviour
{
    public GameObject Cell;
    public Image borderImage;

    private Color originalColor;
    private Color borderColor;

    private void Awake()
    {
        originalColor = Cell.GetComponent<Image>().color;
        ResetBorderColor();
    }

    public void SetBorderColor(Color color)
    {
        borderColor = color;
        borderImage.color = borderColor;
    }

    public void ResetBorderColor()
    {
        borderImage.color = Color.clear;
    }

    public void SetCellColor(Color color)
    {
        Cell.GetComponent<Image>().color = color;
    }

    public void ResetCellColor()
    {
        Cell.GetComponent<Image>().color = originalColor;
    }
}
