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
    }

    private void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);

        if (IsDragging)
        {
            if (hit.collider != null)
            {
                var cellScript = hit.collider.gameObject.GetComponent<CellScript>();
                if (cellScript != null)
                {
                    currentCell = cellScript.gameObject;
                    cellScript.Card = gameObject; // Set the card variable to the card object when the card is touching a cell
                    cellScript.WithCard = true;

                    // Shrink the card to fit the cell size
                    ResizeCardToCell(currentCell);
                }
            }
            else if (currentCell != null)
            {
                var cellScript = currentCell.GetComponent<CellScript>();
                if (cellScript != null)
                {
                    cellScript.Card = null; // Set the card variable to null when the card is not touching a cell
                    cellScript.WithCard = false;
                }
                currentCell = null;

                // Reset the card size to the original
                ResetCardSize();
            }
        }
    }

    private void ResizeCardToCell(GameObject cell)
    {
        // Shrink the card to fit the cell size
        var cellRectTransform = cell.GetComponent<RectTransform>();
        var cardRectTransform = GetComponent<RectTransform>();

        // Set the card size to fit the cell size while maintaining the original proportions
        float cellWidth = cellRectTransform.rect.width;
        float cellHeight = cellRectTransform.rect.height;
        float cardWidth = cardRectTransform.rect.width;
        float cardHeight = cardRectTransform.rect.height;

        float scaleX = cellWidth / cardWidth;
        float scaleY = cellHeight / (cardHeight * 0.9f); // Adjust the height to be slightly shorter than the cell

        float scale = Mathf.Min(scaleX, scaleY);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void ResetCardSize()
    {
        // Reset the card size to the original
        transform.localScale = Vector3.one;
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