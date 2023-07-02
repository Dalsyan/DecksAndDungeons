using UnityEngine;
using UnityEngine.UI;

public class CardScript : MonoBehaviour
{
    public GameObject Card;
    private bool IsDragging = false;
    private Vector2 InitialPosition;
    private bool IsHoveringCell = false;
    private Transform currentCell;


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
    }

    private void Update()
    {
        if (IsDragging)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            // Check if the card is hovering over a cell
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);
            if (hit.collider != null)
            {
                var cell = hit.collider.transform;
                if (cell != currentCell)
                {
                    // Reset previous cell's color if any
                    if (currentCell != null)
                    {
                        var previousCellImage = currentCell.GetComponent<Image>();
                        previousCellImage.color = Color.white;
                    }

                    // Set current cell's color
                    var cellImage = cell.GetComponent<Image>();
                    cellImage.color = Color.yellow;
                    currentCell = cell;
                }
            }
            else if (currentCell != null)
            {
                // Reset cell's color if card is not hovering over any cell
                var cellImage = currentCell.GetComponent<Image>();
                cellImage.color = Color.white;
                currentCell = null;
            }
        }
    }

    public void StartDrag()
    {
        InitialPosition = transform.position;
        IsDragging = true;
        transform.localScale = new Vector3(1.2f, 1.2f, 1f); // Increase card size
    }

    public void EndDrag()
    {
        IsDragging = false;
        transform.localScale = Vector3.one; // Reset card size

        if (currentCell != null)
        {
            // Set the card's parent to the cell
            transform.SetParent(currentCell, false);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            // If no cell is detected, reset the card's position
            transform.position = InitialPosition;
        }
    }

}