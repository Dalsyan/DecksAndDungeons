using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System;
using DG.Tweening;

public class AgentServer : MonoBehaviour
{
    // Instancia de AgentServer
    private static AgentServer instance;
    public static AgentServer Instance => instance;

    // Gestion de mazos
    public int NumPlayerHand { get; set; }
    public int NumEnemyHand { get; set; }
    public int NumPlayerCardsInTable { get; set; }
    public int NumEnemyCardsInTable { get; set; }
    public List<Dictionary<string, object>> PlayerDeck { get; set; }
    public List<Dictionary<string, object>> EnemyDeck { get; set; }
    public List<Dictionary<string, object>> PlayerCardsInTable { get; set; }
    public List<Dictionary<string, object>> EnemyCardsInTable { get; set; }
    public List<Dictionary<string, object>> CardsInTable { get; set; }
    public string SelectedDeck;

    // Gestion de mana
    public int PlayerManaPool { get; set; } = 3;
    public int EnemyManaPool { get; set; } = 3;
    public int CurrentPlayerManaPool { get; set; }
    public int CurrentEnemyManaPool { get; set; }

    // Address y Ports de sockets
    private readonly string Address = "127.0.0.1";
    private readonly int UnityPort = 8000;
    private readonly int SpadePort = 8001;

    // Gestion de sockets
    private TcpListener UnityListener;
    public TcpClient SpadeClient;
    private NetworkStream Stream;

    // sub-threads
    private Thread UnityServerThread;
    private Thread UnityReceiverThread;
    private Thread ProcessThread;

    // Queue de acciones
    private Queue<Action> ActionsQueue;

    // Gestion de clientes
    public bool Open;
    public int nClients = 0;
        
    // Prefabs
    public GameObject PlayerArea;
    public GameObject EnemyArea;
    public GameObject Card;

    // Flags
    public bool GameStart = false;
    public bool PlayerPlayCards = false;
    public bool EnemyPlayCards = false;

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
        CardsInTable = new List<Dictionary<string, object>>();

        CurrentPlayerManaPool = PlayerManaPool;
        CurrentEnemyManaPool = EnemyManaPool;

        SelectedDeck = PlayerPrefs.GetString("SelectedDeck");
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

        if (message == "start")
        {
            var selectedDeck = new Dictionary<string, string>()
            {
                ["action"] = "selectDeck",
                ["data"] = SelectedDeck
            };

            var selectedDeckJson = JsonConvert.SerializeObject(selectedDeck, Formatting.Indented);
            SendMessages(selectedDeckJson);
            SendMessages("deck_selected");
        }

        else if (message == "game_start")
        {
            GameStart = true;
        }

        else
        {
            Dictionary<string, object> messageDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

            if (messageDict.TryGetValue("action", out object actionObj) && messageDict.TryGetValue("data", out object dataObj))
            {
                string action = actionObj.ToString();

                if (dataObj is bool dataBool)
                {
                    switch (action)
                    {
                        case "player_play_cards":
                            PlayerPlayCards = dataBool;
                            break;
                            
                        case "enemy_play_cards":
                            EnemyPlayCards = dataBool;
                            break;

                        default:
                            UnityEngine.Debug.LogWarning("Unknown action.");
                            break;
                    }
                }
                
                else if (dataObj is string dataString)
                {
                    switch (action)
                    {
                        case "move_card":
                            ActionsQueue.Enqueue(() => {
                                var card = GameObject.Find(dataString.ToString());

                                messageDict.TryGetValue("pos", out object pos);
                                var cell = GameObject.Find(pos.ToString());

                                var moved = false;
                                card.transform.DOMove(cell.transform.position, (float)1);
                                moved = true;

                                while (!moved)
                                {
                                    card.transform.SetParent(cell.transform);
                                }
                            });
                            
                            break;

                        case "damage_card":
                            ActionsQueue.Enqueue(() => {
                                var card = GameObject.Find(dataString.ToString());

                                messageDict.TryGetValue("current_hp", out object current_hp);
                                
                                var hp_text = card.GetComponent<CardScript>().HpText.text = current_hp.ToString();
                            });

                            break;

                        case "kill_card":
                            ActionsQueue.Enqueue(() => {
                                var card = GameObject.Find(dataString.ToString());
                                var cardScript = card.GetComponent<CardScript>();

                                int indexToRemove = -1;
                                for (int i = 0; i < CardsInTable.Count; i++)
                                {
                                if (CardsInTable[i].ContainsKey("Name") && CardsInTable[i]["Name"].ToString() == cardScript.Name)
                                    {
                                        indexToRemove = i;
                                        break;
                                    }
                                }
                                if (indexToRemove != -1)
                                {
                                    CardsInTable.RemoveAt(indexToRemove);
                                }

                                int indexToRemove2 = -1;
                                if (card.GetComponent<CardScript>().Owner == "player")
                                {
                                    for (int i = 0; i < PlayerCardsInTable.Count; i++)
                                    {
                                        if (PlayerCardsInTable[i].ContainsKey("Name") && PlayerCardsInTable[i]["Name"].ToString() == cardScript.Name)
                                        {
                                            indexToRemove2 = i;
                                            break;
                                        }
                                    }
                                    if (indexToRemove2 != -1)
                                    {
                                        PlayerCardsInTable.RemoveAt(indexToRemove2);
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < EnemyCardsInTable.Count; i++)
                                    {
                                        if (EnemyCardsInTable[i].ContainsKey("Name") && EnemyCardsInTable[i]["Name"].ToString() == cardScript.Name)
                                        {
                                            indexToRemove2 = i;
                                            break;
                                        }
                                    }
                                    if (indexToRemove2 != -1)
                                    {
                                        EnemyCardsInTable.RemoveAt(indexToRemove2);
                                    }
                                }
                                    

                                Destroy(card);

                                var playerCardsJson = JsonConvert.SerializeObject(PlayerCardsInTable, Formatting.Indented);
                                UnityEngine.Debug.Log($"player cards: {playerCardsJson}");
                                var enemyCardsJson = JsonConvert.SerializeObject(EnemyCardsInTable, Formatting.Indented);
                                UnityEngine.Debug.Log($"enemy cards: {enemyCardsJson}");
                                var CardsJson = JsonConvert.SerializeObject(CardsInTable, Formatting.Indented);
                                UnityEngine.Debug.Log($"cards: {CardsJson}");
                            });

                            break;

                        default:
                            UnityEngine.Debug.LogWarning("Unknown action.");
                            break;
                    }
                }
                
                else if (dataObj is int dataInt)
                {
                    switch (action)
                    {
                        case "":
                            var x = dataInt;
                            break;
                        default:
                            UnityEngine.Debug.LogWarning("Unknown action.");
                            break;
                    }
                }

                else
                {
                    string dataJson = JsonConvert.SerializeObject(dataObj);
                    var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dataJson);

                    switch (action)
                    {
                        case "first_turn":
                            // procesar los roles 'player' y 'enemy' de los jugadores
                            break;

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
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid message format.");
            }
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

