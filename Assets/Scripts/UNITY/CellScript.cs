using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellScript : MonoBehaviour
{
    private GameObject Cell;
    private Image Image;
    private Outline Outline;

    private void Awake()
    {
        Cell = gameObject;
        Image = Cell.GetComponent<Image>();
        Outline = Image.GetComponent<Outline>();
    }

    public void ShowBorder()
    {
        Outline.enabled = true; // Enable the outline component
    }

    public void HideBorder()
    {
        Outline.enabled = false; // Disable the outline component
    }
}