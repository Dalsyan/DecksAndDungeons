using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler

{
    public GameObject Card;
    private Image CardImage;
    private Image Border;
    private Image Imagen;
    private TextMeshProUGUI NameText;
    private TextMeshProUGUI DescText;
    private TextMeshProUGUI ManaText;
    public TextMeshProUGUI HpText;
    private TextMeshProUGUI AcText;
    private TextMeshProUGUI StrText;
    private TextMeshProUGUI ConText;
    private TextMeshProUGUI DexText;
    private TextMeshProUGUI MagText;
    private TextMeshProUGUI DamageText;

    public Transform ParentAfterDrag;
    public Vector3 OriginalSize;

    public string PublicName;
    public string Name;
    public string Class;
    public string Race;
    public string Owner;
    public string Type;
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
    public int power;

    private void Start()
    {
        Card.name = Name;
        PublicName = Race + " " + Class;
        CardImage = transform.GetComponent<Image>();
        OriginalSize = Vector3.one;

        Border = transform.Find("Border").GetComponent<Image>();
        Imagen = Border.transform.Find("Imagen").GetComponent<Image>();

        if (Owner == "player")
        {
            Imagen.GetComponent<Image>().color = Color.cyan;
        }
        else
        {
            Imagen.GetComponent<Image>().color = Color.red;
        }

        NameText = Border.transform.Find("Name").GetComponent<Image>().transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        NameText.text = Name;

        if (Type == "creature")
        {
            ManaText = Border.transform.Find("mana").GetComponent<Image>().transform.Find("manaText").GetComponent<TextMeshProUGUI>();
            ManaText.text = level.ToString();
            HpText = Border.transform.Find("hp").GetComponent<Image>().transform.Find("hpText").GetComponent<TextMeshProUGUI>();
            HpText.text = hp.ToString();
            AcText = Border.transform.Find("ac").GetComponent<Image>().transform.Find("acText").GetComponent<TextMeshProUGUI>();
            AcText.text = ac.ToString();
            //StrText = Border.transform.Find("Attributes").GetComponent<Image>().transform.Find("strText").GetComponent<TextMeshProUGUI>();
            //StrText.text = "STR: \n" + str.ToString();
            //ConText = Border.transform.Find("Attributes").GetComponent<Image>().transform.Find("conText").GetComponent<TextMeshProUGUI>();
            //ConText.text = "CON: \n" + con.ToString();
            //DexText = Border.transform.Find("Attributes").GetComponent<Image>().transform.Find("dexText").GetComponent<TextMeshProUGUI>();
            //DexText.text = "DEX: \n" + dex.ToString();
            MagText = Border.transform.Find("Attributes").GetComponent<Image>().transform.Find("magText").GetComponent<TextMeshProUGUI>();
            MagText.text = "MAG: \n" + magic.ToString();
            DamageText = Border.transform.Find("dmg").GetComponent<Image>().transform.Find("dmgText").GetComponent<TextMeshProUGUI>();
            DamageText.text = damage.ToString();
        }
        else
        {
            ManaText = Border.transform.Find("mana").GetComponent<Image>().transform.Find("manaText").GetComponent<TextMeshProUGUI>();
            ManaText.text = power.ToString();
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