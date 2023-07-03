using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler

{
    public GameObject Card;
    private Image CardImage;
    private GameObject currentCell;
    public bool IsDragging = false;
    public Transform ParentAfterDrag;

    public string Name;
    public string Class;
    public string Race;
    public string Owner;
    public int level;
    public int hp;
    public int ac;
    public int str;
    public int con;
    public int dex;
    public int magic;
    public int range;
    public int prio;

    private void Start()
    {
        Name = Race + " " + Class;
        CardImage = transform.GetComponent<Image>();

        if (Owner == "player")
        {
            transform.GetComponent<Image>().color = Color.cyan;
        }
        else
        {
            transform.GetComponent<Image>().color = Color.red;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = new Vector3(1.2f, 1.2f, 1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        ParentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsFirstSibling();
        CardImage.raycastTarget = false;
    }


    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(ParentAfterDrag);
        CardImage.raycastTarget = true;
    }
}