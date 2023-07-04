using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellScript : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject Card;
    private Image CellImage;

    private void Awake()
    {
        CellImage = GetComponent<Image>();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (transform.childCount > 0)
        {
            CellImage.color = new Color(1f, 0f, 0f, 0.5f);
        }
        else
        {
            CellImage.color = new Color(0f, 1f, 0f, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CellImage.color = Color.clear;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount <= 0)
        {
            Card = eventData.pointerDrag;
            var cardImage = Card.GetComponent<Image>();
            CardScript cardScript = Card.GetComponent<CardScript>();
            cardScript.ParentAfterDrag = transform;
        }
    }
}
