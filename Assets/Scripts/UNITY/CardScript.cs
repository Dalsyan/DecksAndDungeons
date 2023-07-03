using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler

{
    public GameObject Card;
    private Vector2 InitialPosition;
    private GameObject currentCell;

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
        InitialPosition = transform.position;
        Name = Race + " " + Class;
    }

    private void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);

        if (hit.collider != null)
        {
            var cellScript = hit.collider.gameObject.GetComponent<CellScript>();
            if (cellScript != null)
            {
                currentCell = cellScript.gameObject;
                Debug.Log("Im at cell: " + currentCell.name);
                // Perform actions when the card is touching a cell
            }
        }
        else if (currentCell != null)
        {
            currentCell = null;

            // Perform actions when the card is not touching a cell
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

    public void StartDrag()
    {
        InitialPosition = transform.position;
        transform.localScale = new Vector3(1.2f, 1.2f, 1f); // Increase card size
    }

    public void EndDrag()
    {
        transform.localScale = Vector3.one; // Reset card size

        if (currentCell != null)
        {
            // Set the card's parent to the cell
            transform.SetParent(currentCell.transform, true);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            // If no cell is detected, reset the card's position
            transform.position = InitialPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    }
}