using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;

public class GameUtilities : MonoBehaviour
{
    private bool GameIsPaused = false;
    public GameObject PauseMenu;

    public GameObject Server;
    private AgentServer AgentServer;
    public GameObject Card;
    public GameObject PlayerArea;
    public GameObject EnemyArea;
    public List<Dictionary<string, object>> PlayerDeckList;
    public List<Dictionary<string, object>> EnemyDeckList;

    private void Start()
    {
        AgentServer = Server.GetComponent<AgentServer>();
        PlayerDeckList = AgentServer.Instance.PlayerDeck;
        EnemyDeckList = AgentServer.Instance.EnemyDeck;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else 
            {
                Pause(); 
            }
        }
    }

    #region PAUSE MENU

    public void Resume()
    {
        PauseMenu.SetActive(true);
        GameIsPaused = false;
    }

    public void Pause()
    {
        PauseMenu.SetActive(false);
        GameIsPaused = true;
    }

    #region BUTTONS

    public void ExitGame()
    {
        AgentServer.Instance.SendMessages("close");
        Destroy(AgentServer.Instance);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    #endregion

    #endregion

    #region BUTTONS

    public void ClickOnPlayerDeck()
    {
        if (PlayerDeckList.Count > 0 && AgentServer.Instance.NumPlayerHand < 5)
        {
            var card = PlayerDeckList[0];
            string name = card["name"].ToString();
            string cclass = card["class"].ToString();
            string race = card["race"].ToString();
            int level = Convert.ToInt32(card["level"]);
            int hp = Convert.ToInt32(card["hp"]);
            int ac = Convert.ToInt32(card["ac"]);
            int str = Convert.ToInt32(card["str"]);
            int con = Convert.ToInt32(card["con"]);
            int dex = Convert.ToInt32(card["dex"]);
            int damage = Convert.ToInt32(card["damage"]);
            int magic = Convert.ToInt32(card["magic"]);
            int range = Convert.ToInt32(card["range"]);
            PlayerDeckList.RemoveAt(0);

            var playerCard = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
            playerCard.transform.SetParent(PlayerArea.transform, false);

            playerCard.GetComponent<CardScript>().Name = name;
            playerCard.GetComponent<CardScript>().Class = cclass;
            playerCard.GetComponent<CardScript>().Race = race;
            playerCard.GetComponent<CardScript>().Owner = "player";
            playerCard.GetComponent<CardScript>().level = level;
            playerCard.GetComponent<CardScript>().hp = hp;
            playerCard.GetComponent<CardScript>().ac = ac;
            playerCard.GetComponent<CardScript>().str = str;
            playerCard.GetComponent<CardScript>().con = con;
            playerCard.GetComponent<CardScript>().dex = dex;
            playerCard.GetComponent<CardScript>().damage = damage;
            playerCard.GetComponent<CardScript>().magic = magic;
            playerCard.GetComponent<CardScript>().range = range;
            playerCard.GetComponent<CardScript>().prio = dex;

            AgentServer.Instance.NumPlayerHand++;
        }
    }

    public void ClickOnEnemyDeck()
    {
        if (EnemyDeckList.Count > 0 && AgentServer.Instance.NumEnemyHand < 5)
        {
            var card = EnemyDeckList[0];
            string name = card["name"].ToString();
            string cclass = card["class"].ToString();
            string race = card["race"].ToString();
            int level = Convert.ToInt32(card["level"]);
            int hp = Convert.ToInt32(card["hp"]);
            int ac = Convert.ToInt32(card["ac"]);
            int str = Convert.ToInt32(card["str"]);
            int con = Convert.ToInt32(card["con"]);
            int dex = Convert.ToInt32(card["dex"]);
            int damage = Convert.ToInt32(card["damage"]);
            int magic = Convert.ToInt32(card["magic"]);
            int range = Convert.ToInt32(card["range"]);
            EnemyDeckList.RemoveAt(0);

            var enemyCard = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
            enemyCard.transform.SetParent(EnemyArea.transform, false);

            enemyCard.GetComponent<CardScript>().Name = name;
            enemyCard.GetComponent<CardScript>().Class = cclass;
            enemyCard.GetComponent<CardScript>().Race = race;
            enemyCard.GetComponent<CardScript>().Owner = "enemy";
            enemyCard.GetComponent<CardScript>().level = level;
            enemyCard.GetComponent<CardScript>().hp = hp;
            enemyCard.GetComponent<CardScript>().ac = ac;
            enemyCard.GetComponent<CardScript>().str = str;
            enemyCard.GetComponent<CardScript>().con = con;
            enemyCard.GetComponent<CardScript>().dex = dex;
            enemyCard.GetComponent<CardScript>().damage = damage;
            enemyCard.GetComponent<CardScript>().magic = magic;
            enemyCard.GetComponent<CardScript>().range = range;
            enemyCard.GetComponent<CardScript>().prio = dex;

            AgentServer.Instance.NumEnemyHand++;
        }
    }

    public void ClickOnPlayButton()
    {
        if (AgentServer.Instance.PlayerPlayCards)
        {
            foreach (var playerCardInTable in AgentServer.Instance.PlayerCardsInTable)
            {
                Dictionary<string, object> cardData = new()
                {
                    ["action"] = "createPlayerCard",
                    ["data"] = playerCardInTable["Name"],
                    ["pos"] = playerCardInTable["pos"]
                };
                var createPlayerCardActionJson = JsonConvert.SerializeObject(cardData, Formatting.Indented);
                AgentServer.Instance.SendMessages(createPlayerCardActionJson);
            }
            AgentServer.Instance.SendMessages("playerReady");

            AgentServer.Instance.PlayerManaPool++;
            AgentServer.Instance.CurrentPlayerManaPool = AgentServer.Instance.PlayerManaPool;
        }

        if (AgentServer.Instance.EnemyPlayCards)
        {
            foreach (var enemyCardInTable in AgentServer.Instance.EnemyCardsInTable)
            {
                Dictionary<string, object> cardData = new()
                {
                    ["action"] = "createEnemyCard",
                    ["data"] = enemyCardInTable["Name"],
                    ["pos"] = enemyCardInTable["pos"]
                };
                var createEnemyCardActionJson = JsonConvert.SerializeObject(cardData, Formatting.Indented);
                AgentServer.Instance.SendMessages(createEnemyCardActionJson);
            }
            AgentServer.Instance.SendMessages("enemyReady");

            AgentServer.Instance.EnemyManaPool++;
            AgentServer.Instance.CurrentEnemyManaPool = AgentServer.Instance.EnemyManaPool;
        }
    }
    #endregion
}