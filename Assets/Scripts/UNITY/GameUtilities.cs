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

    public GameObject Timer;

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
            string name = card["Name"].ToString();
            string type = card["Type"].ToString();

            string cclass = "";
            string race = "";
            int level = 0;
            int hp = 0;
            int ac = 0;
            int damage = 0;
            int magic = 0;
            int range = 0;
            int power = 0;

            if (type == "creature")
            {
                cclass = card["cclass"].ToString();
                race = card["race"].ToString();
                level = Convert.ToInt32(card["level"]);
                hp = Convert.ToInt32(card["hp"]);
                ac = Convert.ToInt32(card["ac"]);
                damage = Convert.ToInt32(card["damage"]);
                magic = Convert.ToInt32(card["magic"]);
                range = Convert.ToInt32(card["range"]);
            }
            else
            {
                power = Convert.ToInt32(card["power"]);
                level = power;
            }

            PlayerDeckList.RemoveAt(0);

            var playerCard = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
            var cardScript = playerCard.GetComponent<CardScript>();
            playerCard.transform.SetParent(PlayerArea.transform, false);

            cardScript.Name = name;
            cardScript.Type = type;
            cardScript.Class = cclass;
            cardScript.Race = race;
            cardScript.Owner = "player";
            cardScript.level = level;
            cardScript.hp = hp;
            cardScript.ac = ac;
            cardScript.damage = damage;
            cardScript.magic = magic;
            cardScript.range = range;
            cardScript.power = power;

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
            AgentServer.Instance.SendMessages("playerReady");

            AgentServer.Instance.PlayerManaPool++;
            AgentServer.Instance.CurrentPlayerManaPool = AgentServer.Instance.PlayerManaPool;
        }

        Timer.SetActive(true);
        AgentServer.Instance.IsTimer = true;
    }
    #endregion
}