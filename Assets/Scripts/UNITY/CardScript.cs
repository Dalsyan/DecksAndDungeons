using UnityEngine;
using UnityEngine.UI;

public class CardScript : MonoBehaviour
{
    public GameObject Card;
    private bool IsDragging = false;
    private Vector2 InitialPosition;
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

    private void OnMouseOver()
    {
        if (!IsDragging)
        {
            transform.localScale = new Vector3(1.2f, 1.2f, 1f); // Increase card size
        }
    }

    private void OnMouseExit()
    {
        if (!IsDragging)
        {
            transform.localScale = Vector3.one; // Reset card size
        }
    }

    private void Update()
    {
        var hit = Physics2D.Raycast(transform.position, Vector2.zero);
        
        if (IsDragging)
        {
            // While dragging the card, move it with the mouse
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (hit.collider != null)
            {
                // When the card collides with a cell, the cell image creates borders
                if (hit.collider.gameObject.TryGetComponent<CellScript>(out var cellScript))
                {
                    cellScript.ShowBorder(); // Show the border of the cell

                    // Shrink the card to fit the cell size
                    ResizeCardToCell(hit.collider.gameObject);
                }
            }
            else if (currentCell != null)
            {
                // If the card stops colliding with a cell, the cell image returns to the original color
                if (currentCell.TryGetComponent<CellScript>(out var cellScript))
                {
                    cellScript.HideBorder(); // Hide the border of the cell
                }

                // Reset the card size to the original
                ResetCardSize();
                currentCell = null;
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
        float scaleY = cellHeight / cardHeight;

        float scale = Mathf.Min(scaleX, scaleY);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void ResetCardSize()
    {
        // Reset the card size to the original
        transform.localScale = Vector3.one;
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
            transform.SetParent(currentCell.transform, true);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            // If no cell is detected, reset the card's position
            transform.position = InitialPosition;
        }
    }
}