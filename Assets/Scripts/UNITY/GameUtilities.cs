using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUtilities : MonoBehaviour
{
    private string Address = "127.0.0.1";
    private int SpadePort = 8001;

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
            UnityEngine.Debug.Log(AgentServer.Instance.NumPlayerHand < 5);
            var card = PlayerDeckList[0];
            string cclass = card["class"].ToString();
            string race = card["race"].ToString();
            int level = Convert.ToInt32(card["level"]);
            int hp = Convert.ToInt32(card["hp"]);
            int ac = Convert.ToInt32(card["ac"]);
            int str = Convert.ToInt32(card["str"]);
            int dex = Convert.ToInt32(card["dex"]);
            int magic = Convert.ToInt32(card["magic"]);
            int range = Convert.ToInt32(card["range"]);
            int prio = Convert.ToInt32(card["prio"]);
            PlayerDeckList.RemoveAt(0);

            var playerCard = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
            playerCard.transform.SetParent(PlayerArea.transform, false);

            playerCard.GetComponent<CardScript>().Class = cclass;
            playerCard.GetComponent<CardScript>().Race = race;
            playerCard.GetComponent<CardScript>().Owner = "player";
            playerCard.GetComponent<CardScript>().level = level;
            playerCard.GetComponent<CardScript>().hp = hp;
            playerCard.GetComponent<CardScript>().ac = ac;
            playerCard.GetComponent<CardScript>().str = str;
            playerCard.GetComponent<CardScript>().dex = dex;
            playerCard.GetComponent<CardScript>().magic = magic;
            playerCard.GetComponent<CardScript>().range = range;
            playerCard.GetComponent<CardScript>().prio = prio;

            AgentServer.Instance.NumPlayerHand++;
        }
    }

    public void ClickOnEnemyDeck()
    {
        if (EnemyDeckList.Count > 0 && AgentServer.Instance.NumEnemyHand < 5)
        {
            var card = EnemyDeckList[0];
            string cclass = card["class"].ToString();
            string race = card["race"].ToString();
            int level = Convert.ToInt32(card["level"]);
            int hp = Convert.ToInt32(card["hp"]);
            int ac = Convert.ToInt32(card["ac"]);
            int str = Convert.ToInt32(card["str"]);
            int dex = Convert.ToInt32(card["dex"]);
            int magic = Convert.ToInt32(card["magic"]);
            int range = Convert.ToInt32(card["range"]);
            int prio = Convert.ToInt32(card["prio"]);
            EnemyDeckList.RemoveAt(0);

            var enemyCard = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
            enemyCard.transform.SetParent(EnemyArea.transform, false);

            enemyCard.GetComponent<CardScript>().Class = cclass;
            enemyCard.GetComponent<CardScript>().Race = race;
            enemyCard.GetComponent<CardScript>().Owner = "enemy";
            enemyCard.GetComponent<CardScript>().level = level;
            enemyCard.GetComponent<CardScript>().hp = hp;
            enemyCard.GetComponent<CardScript>().ac = ac;
            enemyCard.GetComponent<CardScript>().str = str;
            enemyCard.GetComponent<CardScript>().dex = dex;
            enemyCard.GetComponent<CardScript>().magic = magic;
            enemyCard.GetComponent<CardScript>().range = range;
            enemyCard.GetComponent<CardScript>().prio = prio;

            AgentServer.Instance.NumEnemyHand++;
        }
    }

    public void ClickOnPlayButton()
    {

    }

    #endregion
}

