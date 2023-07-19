using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler

{
    public GameObject Card;
    private Image CardImage;
    public Transform ParentAfterDrag;
    public Vector3 OriginalSize;

    public string PublicName;
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
    public int damage;
    public int magic;
    public int range;
    public int prio;
    public string pos;

    private void Start()
    {
        Card.name = Name;
        PublicName = Race + " " + Class;
        CardImage = transform.GetComponent<Image>();
        OriginalSize = Vector3.one;

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
        transform.localScale = OriginalSize;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Owner == "player" && AgentServer.Instance.PlayerPlayCards)
        {
            transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            ParentAfterDrag = transform.parent;
            transform.SetParent(transform.root);
            transform.SetAsFirstSibling();
            CardImage.raycastTarget = false;
        }

        if (Owner == "enemy" && AgentServer.Instance.EnemyPlayCards)
        {
            transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            ParentAfterDrag = transform.parent;
            transform.SetParent(transform.root);
            transform.SetAsFirstSibling();
            CardImage.raycastTarget = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Owner == "player" && AgentServer.Instance.PlayerPlayCards)
        {
            transform.position = Input.mousePosition;
        }

        if (Owner == "enemy" && AgentServer.Instance.EnemyPlayCards)
        {
            transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Owner == "player" && AgentServer.Instance.PlayerPlayCards)
        {
            transform.SetParent(ParentAfterDrag);
            CardImage.raycastTarget = true;
        }

        if (Owner == "enemy" && AgentServer.Instance.EnemyPlayCards)
        {
            transform.SetParent(ParentAfterDrag);
            CardImage.raycastTarget = true;
        }
    }
}