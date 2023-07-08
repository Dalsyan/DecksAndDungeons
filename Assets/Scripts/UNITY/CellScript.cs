using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    public GameObject Server;
    private AgentServer AgentServer;

    private void Awake()
    {
        AgentServer = Server.GetComponent<AgentServer>();
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
            var cardScript = Card.GetComponent<CardScript>();

            var card = new Dictionary<string, object>()
            {
                ["Name"] = cardScript.Name,
                ["class"] = cardScript.Class,
                ["race"] = cardScript.Race,
                ["level"] = cardScript.level,
                ["hp"] = cardScript.hp,
                ["ac"] = cardScript.ac,
                ["str"] = cardScript.str,
                ["con"] = cardScript.con,
                ["dex"] = cardScript.dex,
                ["damage"] = cardScript.damage,
                ["magic"] = cardScript.magic,
                ["range"] = cardScript.range,
                ["prio"] = cardScript.prio
            };
            var newCard = card;
            newCard.Add("pos", transform.name);
            if (cardScript.Owner == "player")
            {
                if (AgentServer.Instance.NumPlayerCardsInTable < 3)
                {
                    cardScript.ParentAfterDrag = transform;
                    AgentServer.Instance.PlayerDeck.Remove(card);
                    AgentServer.Instance.NumPlayerHand--;
                    AgentServer.Instance.PlayerCardsInTable.Add(newCard);
                    AgentServer.Instance.NumPlayerCardsInTable++;
                }
            }
            else
            {
                if (AgentServer.Instance.NumEnemyCardsInTable < 3)
                {
                    cardScript.ParentAfterDrag = transform;
                    AgentServer.Instance.EnemyDeck.Remove(card);
                    AgentServer.Instance.NumEnemyHand--;
                    AgentServer.Instance.EnemyCardsInTable.Add(newCard);
                    AgentServer.Instance.NumEnemyCardsInTable++;
                }
            }
        }
    }
}
