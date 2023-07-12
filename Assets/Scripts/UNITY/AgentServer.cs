using System.Collections.Generic;
using UnityEngine;

using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System;

public class AgentServer : MonoBehaviour
{
    private static AgentServer instance;
    public static AgentServer Instance => instance;
    public int NumPlayerHand { get; set; }
    public int NumEnemyHand { get; set; }
    public int NumPlayerCardsInTable { get; set; }
    public int NumEnemyCardsInTable { get; set; }
    public List<Dictionary<string, object>> PlayerDeck { get; set; }
    public List<Dictionary<string, object>> EnemyDeck { get; set; }
    public List<Dictionary<string, object>> PlayerCardsInTable { get; set; }
    public List<Dictionary<string, object>> EnemyCardsInTable { get; set; }

    private string Address = "127.0.0.1";
    private int UnityPort = 8000;
    private int SpadePort = 8001;

    private TcpListener UnityListener;
    public TcpClient SpadeClient;
    private NetworkStream Stream;

    private Thread UnityServerThread;
    private Thread UnityReceiverThread;
    private Thread ProcessThread;
    private Queue<Action> ActionsQueue;

    public bool Open;
    public int nClients = 0;

    public GameObject PlayerArea;
    public GameObject EnemyArea;
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

    void Start()
    {
        Open = true;
        ActionsQueue = new Queue<Action>();
        OpenCMDThread();
        ListenUnityThread();

        NumPlayerHand = 0;
        NumEnemyHand = 0;
        NumPlayerCardsInTable = 0;
        NumEnemyCardsInTable = 0;
        PlayerDeck = new List<Dictionary<string, object>>();
        EnemyDeck = new List<Dictionary<string, object>>();
        PlayerCardsInTable = new List<Dictionary<string, object>>();
        EnemyCardsInTable = new List<Dictionary<string, object>>();
    }

    public void Update()
    {
        while (ActionsQueue.Count > 0)
        {
            Action action = ActionsQueue.Dequeue();
            action.Invoke();
        }
    }

    private void OnDestroy()
    {
        SendMessages("close");
        UnityListener?.Stop();
    }

    /// <summary>
    /// Creo hilo para escuchar en el servidor Unity.
    /// </summary>
    private void ListenUnityThread()
    {
        UnityServerThread = new Thread(new ThreadStart(ListenUnity)) { IsBackground = true };
        UnityServerThread.Start();
    }

    /// <summary>
    /// Escucho en la direccion y puerto.
    /// Acepto clientes.
    /// Creo un hilo para enviar y recibir los comandos.
    /// </summary>
    private void ListenUnity()
    {
        try
        {
            UnityListener = new TcpListener(IPAddress.Parse(Address), UnityPort);
            UnityListener.Start();
            UnityEngine.Debug.Log("Server is listening");

            var client = UnityListener.AcceptTcpClient();
            Stream = client.GetStream();
            nClients++;

            UnityReceiverThread = new Thread(() =>
            {
                try
                {
                    RecvMessages();
                    client.Close();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("Exception in receiver thread: " + e.Message);
                }
            });

            UnityReceiverThread.Start();
        }
        catch (SocketException socketException)
        {
            UnityEngine.Debug.Log("SocketException " + socketException.ToString());
        }
        finally
        {
            UnityListener?.Stop();
        }
    }

    private void RecvMessages()
    {
        while (Open)
        {
            if (Stream.DataAvailable)
            {
                var buffer = new byte[1024 * 3];
                int bytesRead = Stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                ProcessMessage(message);
            }
        }
    }

    private void ProcessMessage(string message)
    {
        UnityEngine.Debug.Log($"Received message: {message}");
        Dictionary<string, object> messageDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

        if (messageDict.TryGetValue("action", out object actionObj) && messageDict.TryGetValue("data", out object dataObj))
        {
            string action = actionObj.ToString();
            string dataJson = JsonConvert.SerializeObject(dataObj);

            var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dataJson);
            switch (action)
            {
                case "create_player_deck":
                    CreateDeck("player", dataList);
                    break;
                case "create_enemy_deck":
                    CreateDeck("enemy", dataList);
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

    public void SendMessages(string message)
    {
        try
        {
            SpadeClient = new TcpClient(Address, SpadePort);

            var stream = SpadeClient.GetStream();

            var data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            UnityEngine.Debug.Log($"He enviado un mensaje: {message}, con tamanyo: {data.Length}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error sending message: " + e.Message);
        }
    }

private void CreateDeck(string sender, List<Dictionary<string, object>> data)
    {
        ActionsQueue.Enqueue(() =>
        {
            foreach (Dictionary<string, object> cardDataDict in data)
            {
                if (cardDataDict is null)
                {
                    UnityEngine.Debug.LogWarning("Invalid card data format.");
                    return;
                }

                string name = cardDataDict["name"].ToString();
                string cclass = cardDataDict["class"].ToString();
                string race = cardDataDict["race"].ToString();
                int level = Convert.ToInt32(cardDataDict["level"]);
                int hp = Convert.ToInt32(cardDataDict["hp"]);
                int ac = Convert.ToInt32(cardDataDict["ac"]);
                int str = Convert.ToInt32(cardDataDict["str"]);
                int con = Convert.ToInt32(cardDataDict["con"]);
                int dex = Convert.ToInt32(cardDataDict["dex"]);
                int damage = Convert.ToInt32(cardDataDict["damage"]);
                int magic = Convert.ToInt32(cardDataDict["magic"]);
                int range = Convert.ToInt32(cardDataDict["range"]);

                if (sender == "player")
                {
                    if (NumPlayerHand < 5)
                    {
                        var card = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
                        card.GetComponent<CardScript>().Owner = sender;
                        card.transform.SetParent(PlayerArea.transform, false);
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
                        NumPlayerHand++;
                    }
                    else
                    {
                        Dictionary<string, object> cardData = new()
                        {
                            ["name"] = name,
                            ["class"] = cclass,
                            ["race"] = race,
                            ["level"] = level,
                            ["hp"] = hp,
                            ["ac"] = ac,
                            ["str"] = str,
                            ["con"] = con,
                            ["dex"] = dex,
                            ["damage"] = damage,
                            ["magic"] = magic,
                            ["range"] = range,
                            ["prio"] = dex,
                        };
                        PlayerDeck.Add(cardData);
                    }
                }
                else if (sender == "enemy")
                {
                    if (NumEnemyHand < 5)
                    {
                        var card = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
                        card.GetComponent<CardScript>().Owner = sender;
                        card.transform.SetParent(EnemyArea.transform, false);
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
                        NumEnemyHand++;
                    }
                    else
                    {
                        Dictionary<string, object> cardData = new()
                        {
                            ["name"] = name,
                            ["class"] = cclass,
                            ["race"] = race,
                            ["level"] = level,
                            ["hp"] = hp,
                            ["ac"] = ac,
                            ["str"] = str,
                            ["con"] = con,
                            ["dex"] = dex,
                            ["damage"] = damage,
                            ["magic"] = magic,
                            ["range"] = range,
                            ["prio"] = dex,
                        };
                        EnemyDeck.Add(cardData);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Invalid deck owner specified in the data.");
                    return;
                }
            }
        });
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
        var startInfo = new ProcessStartInfo("cmd.exe", "/K \"python Assets/Scripts/SPADE/AgenteManager.py\"")
        {
            WindowStyle = ProcessWindowStyle.Minimized
            //CreateNoWindow = true,
            //UseShellExecute = false
        };
        Process.Start(startInfo);
    }
}

