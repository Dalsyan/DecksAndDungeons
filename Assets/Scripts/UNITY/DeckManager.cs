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
using UnityEngine;

public class DeckManager : MonoBehaviour
{
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

    public void SendMessages(string message)
    {
        try
        {
            OwlClient = new TcpClient(Address, OwlPort);

            UnityEngine.Debug.Log($"He establecido conexion");

            var stream = OwlClient.GetStream();

            UnityEngine.Debug.Log($"He encontrado el stream");

            var data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);

            UnityEngine.Debug.Log($"data: {data}");

            var buffer = new byte[1024 * 3];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            var respone = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            ProcessMessage(message);

            UnityEngine.Debug.Log($"He enviado un mensaje: {message}, con tamanyo: {data.Length}");
        }
        catch (Exception e)
        {
           UnityEngine.Debug.LogError("Error sending message: " + e.Message);
        }
    }

    private void ProcessMessage(string message)
    {
        UnityEngine.Debug.Log($"Received message: {message}");

        if (message == "ok")
        {
            UnityEngine.Debug.Log("You created you account correctly!");
        }

        else
        {
            Dictionary<string, object> messageDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

            if (messageDict.TryGetValue("action", out object actionObj) && messageDict.TryGetValue("data", out object dataObj))
            {
                string action = actionObj.ToString();

                if (dataObj is List<object> dataList)
                {
                    var isListOfDictionaries = dataList.All(item => item is Dictionary<string, object>);
                    var isListOfStrings = dataList.All(item => item is string);

                    if (isListOfDictionaries)
                    {
                        string dataJson = JsonConvert.SerializeObject(dataObj);
                        var dataDict = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dataJson);

                        switch (action)
                        {
                            case "show_cards":
                                ShowCards(dataDict);
                                break;

                            default:
                                UnityEngine.Debug.LogWarning("Unknown action.");
                                break;
                        }
                    }

                    if (isListOfStrings)
                    {
                        string dataJson = JsonConvert.SerializeObject(dataObj);
                        var dataString = JsonConvert.DeserializeObject<List<string>>(dataJson);

                        switch (action)
                        {
                            case "show_decks":
                                ShowDecks(dataString);
                                break;

                            default:
                                UnityEngine.Debug.LogWarning("Unknown action.");
                                break;
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("dataObj is not a list.");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid message format.");
            }
        }
    }

    private void ShowDecks(List<string> dataList)
    {
        foreach (string deckName in dataList)
        {
            if (deckName is null)
            {
                UnityEngine.Debug.LogWarning("Invalid deck data format.");
                return;
            }
        }
    }

    private void ShowCards(List<Dictionary<string, object>> dataList)
    {
        foreach (Dictionary<string, object> cardDict in dataList)
        {
            if (cardDict is null)
            {
                UnityEngine.Debug.LogWarning("Invalid deck data format.");
                return;
            }

            string name = cardDict["name"].ToString();
            string cclass = cardDict["class"].ToString();
            string race = cardDict["race"].ToString();
            int level = Convert.ToInt32(cardDict["level"]);
            int hp = Convert.ToInt32(cardDict["hp"]);
            int ac = Convert.ToInt32(cardDict["ac"]);
            int str = Convert.ToInt32(cardDict["str"]);
            int con = Convert.ToInt32(cardDict["con"]);
            int dex = Convert.ToInt32(cardDict["dex"]);
            int damage = Convert.ToInt32(cardDict["damage"]);
            int magic = Convert.ToInt32(cardDict["magic"]);
            int range = Convert.ToInt32(cardDict["range"]);

            var card = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
            card.GetComponent<CardScript>().Name = name;
            card.GetComponent<CardScript>().Class = cclass;
            card.GetComponent<CardScript>().Race = race;
            card.GetComponent<CardScript>().level = level;
            card.GetComponent<CardScript>().hp = hp;
            card.GetComponent<CardScript>().ac = ac;
            card.GetComponent<CardScript>().str = str;
            card.GetComponent<CardScript>().con = con;
            card.GetComponent<CardScript>().dex = dex;
            card.GetComponent<CardScript>().damage = damage;
            card.GetComponent<CardScript>().magic = magic;
            card.GetComponent<CardScript>().range = range;
            card.GetComponent<CardScript>().prio = dex;
            //card.transform.SetParent(DeckMenu.transform, false);
        }
    }

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
}
