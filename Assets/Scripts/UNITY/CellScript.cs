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
    public bool WithCard = false;
    public bool HasCard = false;

    private Image CellImage;

    private void Awake()
    {
        CellImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (transform.childCount > 0)
        {
            HasCard = true;
        }
        else
        {
            HasCard = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (WithCard)
        {
            if (HasCard)
            {
                CellImage.color = new Color(1f, 0f, 0f, 0.5f);
            }
            else
            {
                CellImage.color = new Color(0f, 1f, 0f, 0.5f);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (WithCard)
        {
            CellImage.color = Color.clear;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Card = eventData.pointerDrag;
        CardScript cardScript = Card.GetComponent<CardScript>();
        cardScript.ParentAfterDrag = transform;
    }
}
