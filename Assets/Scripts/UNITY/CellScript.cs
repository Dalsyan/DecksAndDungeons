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
        if (AgentServer.Instance.PlayerPlayCards || AgentServer.Instance.EnemyPlayCards)
        {
            Card = eventData.pointerDrag;
            var cardScript = Card.GetComponent<CardScript>();

            var card = new Dictionary<string, object>()
            {
                ["Name"] = cardScript.Name,
                ["Type"] = cardScript.Type,
                ["Owner"] = cardScript.Owner,
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

            if (card["Type"] is string type)
            {
                if (type == "creature")
                {
                    if (transform.childCount <= 0)
                    {
                        var newCard = card;
                        newCard.Add("pos", transform.name);
                        if (cardScript.Owner == "player")
                        {
                            if (cardScript.level <= AgentServer.Instance.CurrentPlayerManaPool)
                            {
                                cardScript.ParentAfterDrag = transform;
                                AgentServer.Instance.PlayerDeck.Remove(card);
                                AgentServer.Instance.NumPlayerHand--;
                                AgentServer.Instance.PlayerCardsInTable.Add(newCard);
                                AgentServer.Instance.NumPlayerCardsInTable++;
                                AgentServer.Instance.CurrentPlayerManaPool -= cardScript.level;
                            }
                        }
                        else
                        {
                            if (cardScript.level <= AgentServer.Instance.CurrentEnemyManaPool)
                            {
                                cardScript.ParentAfterDrag = transform;
                                AgentServer.Instance.EnemyDeck.Remove(card);
                                AgentServer.Instance.NumEnemyHand--;
                                AgentServer.Instance.EnemyCardsInTable.Add(newCard);
                                AgentServer.Instance.NumEnemyCardsInTable++;
                                AgentServer.Instance.CurrentEnemyManaPool -= cardScript.level;
                            }
                        }
                        AgentServer.Instance.CardsInTable.Add(newCard);
                    }
                }
                else
                {
                    var newCard = card;
                    newCard.Add("pos", transform.name);
                    if (cardScript.Owner == "player")
                    {
                        if (cardScript.level <= AgentServer.Instance.CurrentPlayerManaPool)
                        {
                            Dictionary<string, object> cardData = new()
                            {
                                ["action"] = "createPlayerCard",
                                ["data"] = newCard["Name"],
                                ["pos"] = newCard["pos"]
                            };
                            var createPlayerCardActionJson = JsonConvert.SerializeObject(cardData, Formatting.Indented);
                            AgentServer.Instance.SendMessages(createPlayerCardActionJson);

                            AgentServer.Instance.PlayerDeck.Remove(card);
                            AgentServer.Instance.NumPlayerHand--;
                            AgentServer.Instance.CurrentPlayerManaPool -= cardScript.level;
                        }
                    }
                    else
                    {
                        if (cardScript.level <= AgentServer.Instance.CurrentEnemyManaPool)
                        {
                            Dictionary<string, object> cardData = new()
                            {
                                ["action"] = "createEnemyCard",
                                ["data"] = newCard["Name"],
                                ["pos"] = newCard["pos"]
                            };
                            var createEnemyCardActionJson = JsonConvert.SerializeObject(cardData, Formatting.Indented);
                            AgentServer.Instance.SendMessages(createEnemyCardActionJson);

                            AgentServer.Instance.EnemyDeck.Remove(card);
                            AgentServer.Instance.NumEnemyHand--;
                            AgentServer.Instance.CurrentEnemyManaPool -= cardScript.level;
                        }
                    }
                }
            }
        }
    }
}
