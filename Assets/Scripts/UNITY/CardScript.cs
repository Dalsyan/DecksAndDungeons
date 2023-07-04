using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler

{
    public GameObject Card;
    private Image CardImage;
    public Transform ParentAfterDrag;
    public Vector3 OriginalSize;
    public AgentServer AgentServer;

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
        OriginalSize = Vector3.one;
        AgentServer = new AgentServer();

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

        var card = new Dictionary<string, object>()
        {
            ["Name"] = Name,
            ["class"] = Class,
            ["race"] = Race,
            ["level"] = level,
            ["hp"] = hp,
            ["ac"] = ac,
            ["str"] = str,
            // ["con"] = con,
            ["dex"] = dex,
            ["magic"] = magic,
            ["range"] = range,
            ["prio"] = prio,
            ["pos"] = transform.parent.name
        };
        if (Owner == "player")
        {
            AgentServer.PlayersInTable.Add(card);
            AgentServer.PlayersTable++;
        }
        else
        {
            AgentServer.EnemiesInTable.Add(card);
            AgentServer.EnemiesTable++;
        }
    }
}