using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellScript : MonoBehaviour //, IPointerEnterHandler, IPointerExitHandler
{
    private Image cellImage;
    private Outline cellOutline;
    private bool isCollidingWithCard;

    private void Awake()
    {
        cellImage = GetComponent<Image>();
        cellOutline = GetComponent<Outline>();
    }

    public void ShowOutline()
    {
        isCollidingWithCard = true;
        cellOutline.enabled = true;
    }

    public void HideOutline()
    {
        isCollidingWithCard = false;
        cellOutline.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Card"))
        {
            ShowOutline();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Card"))
        {
            HideOutline();
        }
    }
}
