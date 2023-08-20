using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    #region VARIABLES 

    // Instancia de DeckManager
    private static DeckManager instance;
    public static DeckManager Instance => instance;

    // Address y Ports de sockets
    private readonly string Address = "127.0.0.1";
    private readonly int OwlPort = 8002;

    // Gestion de sockets
    public TcpClient OwlClient;

    // sub-threads
    private Thread ProcessThread;

    // Prefabs
    public GameObject Card;

    // Gestion de usuario 
    public bool Logged { get; set; } = false;
    public bool SelectedDeck { get; set; } = false;

    // Gestion de mazos y cartas
    public List<string> DeckNames = new List<string>();
    public List<string> CardNames = new List<string>();

    // Menus
    public GameObject UserMenu;

    #endregion

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        OpenCMDThread();
    }

    private void OnDestroy()
    {
        SendMessages("close");
    }

    #region SOCKETS

    public void SendMessages(string message)
    {
        try
        {
            OwlClient = new TcpClient(Address, OwlPort);

            var stream = OwlClient.GetStream();

            var data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);

            UnityEngine.Debug.Log($"He enviado un mensaje: {message}, con tamanyo: {data.Length}");

            var buffer = new byte[1024 * 3];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (!string.IsNullOrEmpty(response))
            {
                ProcessMessage(response);
            }
        }
        catch (Exception e)
        {
           UnityEngine.Debug.LogError("Error sending message: " + e.Message);
        }
    }

    private void ProcessMessage(string message)
    {
        UnityEngine.Debug.Log($"Received message: {message}");

        if (message == "registered")
        {
            UnityEngine.Debug.Log("You created you account correctly!");
        }
        
        else if (message == "logged")
        {
            UnityEngine.Debug.Log("You logged correctly!");
            Logged = true;
        }

        else
        {
            Dictionary<string, object> messageDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

            if (messageDict.TryGetValue("action", out object actionObj) && messageDict.TryGetValue("data", out object dataObj))
            {
                string action = actionObj.ToString();
                string dataJson = JsonConvert.SerializeObject(dataObj);

                switch (action)
                {
                    case "logged":
                        UnityEngine.Debug.Log("You logged correctly!");
                        Logged = true;

                        LogAction(dataJson);

                        break;

                    case "show_cards":
                        ShowCards(dataJson);
                        break;

                    case "show_decks":
                        ShowDecks(dataJson);
                        break;

                    case "create_deck":
                        UnityEngine.Debug.Log("Deck created succesfully!");
                        break;

                    default:
                        UnityEngine.Debug.LogWarning("Unknown action.");
                        break;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid message format.");
            }
        }
    }

    #endregion

    private void LogAction(string dataJson)
    {
        var dataDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(dataJson);

        int wins = Convert.ToInt32(dataDict["wins"]);
        int loses = Convert.ToInt32(dataDict["loses"]);

        PlayerPrefs.SetInt("Wins", wins);
        PlayerPrefs.SetInt("Loses", loses);

        var winsText = UserMenu.transform.Find("Wins").GetComponentInChildren<TMP_Text>();
        var losesText = UserMenu.transform.Find("Loses").GetComponentInChildren<TMP_Text>();
        winsText.text = wins.ToString();
        losesText.text = loses.ToString();
    }

    #region CARD MANAGEMENT

    private void ShowDecks(string dataJson)
    {
        var dataList = JsonConvert.DeserializeObject<List<object>>(dataJson);

        foreach (string deckName in dataList)
        {
            if (deckName is null)
            {
                UnityEngine.Debug.LogWarning("Invalid deck data format.");
                return;
            }

            if (DeckNames.Any(x => x == deckName))
            {
                continue;
            }

            DeckNames.Add(deckName);
        }
    }

    private void ShowCards(string dataJson)
    {
        var dataList = JsonConvert.DeserializeObject<List<object>>(dataJson);

        foreach (object dataObject in dataList)
        {
            string cardJson = JsonConvert.SerializeObject(dataObject);
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(cardJson);

            if (dataDict is Dictionary<string, object> cardDict)
            {
                if (cardDict is null)
                {
                    UnityEngine.Debug.LogWarning("Invalid deck data format.");
                    return;
                }

                string name = cardDict["name"].ToString();
                int level = Convert.ToInt32(cardDict["level"]);
                int hp = Convert.ToInt32(cardDict["hp"]);
                int ac = Convert.ToInt32(cardDict["ac"]);
                int damage = Convert.ToInt32(cardDict["damage"]);

                if (DeckNames.Any(x => x == name))
                {
                    continue;
                }

                CardNames.Add(name);
            }
        }
    }

    #endregion 

    #region PYTHON THREAD

    /// <summary>
    /// Creo hilo para abrir CMD
    /// </summary>
    private void OpenCMDThread()
    {
        ProcessThread = new Thread(new ThreadStart(OpenCMD)) { IsBackground = true };
        ProcessThread.Start();
    }

    /// <summary>
    /// Abro CMD y ejecuto el AgentManager
    /// </summary>
    private void OpenCMD()
    {
        var startInfo = new ProcessStartInfo("cmd.exe", "/K \"python Assets/Scripts/SPADE/OwlManager.py\"")
        {
            WindowStyle = ProcessWindowStyle.Minimized
            //CreateNoWindow = true,
            //UseShellExecute = false
        };
        Process.Start(startInfo);
    }

    #endregion
}
