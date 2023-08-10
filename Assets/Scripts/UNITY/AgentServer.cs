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
using UnityEngine.SocialPlatforms;
using System.Linq;
using UnityEditor;

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
    public List<Dictionary<string, object>> PlayerHand { get; set; }
    public List<Dictionary<string, object>> EnemyHand { get; set; }
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

        PlayerHand = new List<Dictionary<string, object>>();
        EnemyHand = new List<Dictionary<string, object>>();
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
            StartAction();
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

                            if (PlayerPlayCards)
                                CardsToArea();

                            break;
                            
                        case "enemy_play_cards":
                            EnemyPlayCards = dataBool;

                            if (EnemyPlayCards)
                            {
                                CardsToArea();
                                EnemyPlayCardsAction();
                            }

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
                            MoveCardAction(messageDict, dataString);
                            break;

                        case "damage_card":
                            DamageCardAction(messageDict, dataString);
                            break;

                        case "kill_card":
                            KillCardAction(messageDict, dataString);
                            break;

                        default:
                            UnityEngine.Debug.LogWarning("Unknown action.");
                            break;
                    }
                }

                /* else if (dataObj is int dataInt)
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
                */

                else
                {
                    string dataJson = JsonConvert.SerializeObject(dataObj);
                    var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dataJson);

                    switch (action)
                    {
                        /* case "first_turn":
                        case "first_turn":
                            // procesar los roles 'player' y 'enemy' de los jugadores
                            break;
                        */

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

    private void StartAction()
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

    private void MoveCardAction(Dictionary<string, object> messageDict, string dataString)
    {
        ActionsQueue.Enqueue(() => {
            var card = GameObject.Find(dataString.ToString());

            messageDict.TryGetValue("pos", out object pos);
            var cell = GameObject.Find(pos.ToString());

            card.transform.DOMove(cell.transform.position, (float)0.15)
                .OnComplete(() => {
                    card.transform.SetParent(cell.transform);
                });
        });
    }

    private void DamageCardAction(Dictionary<string, object> messageDict, string dataString)
    {
        ActionsQueue.Enqueue(() => {
            var card = GameObject.Find(dataString.ToString());

            messageDict.TryGetValue("current_hp", out object current_hp);

            var hp_text = card.GetComponent<CardScript>().HpText.text = current_hp.ToString();
        });
    }

    private void KillCardAction(Dictionary<string, object> messageDict, string dataString)
    {
        ActionsQueue.Enqueue(() => {
            var card = GameObject.Find(dataString.ToString());
            var cardScript = card.GetComponent<CardScript>();

            // Remover de CardsInTable
            CardsInTable.RemoveAll(c => c.ContainsKey("Name") && c["Name"].ToString() == cardScript.Name);

            // Determinar la lista específica y remover la carta
            List<Dictionary<string, object>> targetList = cardScript.Owner == "player" ? PlayerCardsInTable : EnemyCardsInTable;
            targetList.RemoveAll(c => c.ContainsKey("Name") && c["Name"].ToString() == cardScript.Name);

            Destroy(card);
        });
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

                string type = cardDataDict["type"].ToString();
                string name = cardDataDict["name"].ToString();
                string cclass = null;
                string race = null;
                int level = 0;
                int hp = 0;
                int ac = 0;
                int damage = 0;
                int magic = 0;
                int range = 0;
                int power = 0;

                if (type == "creature")
                {
                    cclass = cardDataDict["class"].ToString();
                    race = cardDataDict["race"].ToString();
                    level = Convert.ToInt32(cardDataDict["level"]);
                    hp = Convert.ToInt32(cardDataDict["hp"]);
                    ac = Convert.ToInt32(cardDataDict["ac"]);
                    damage = Convert.ToInt32(cardDataDict["damage"]);
                    magic = Convert.ToInt32(cardDataDict["magic"]);
                    range = Convert.ToInt32(cardDataDict["range"]);
                }
                else
                {
                    power = Convert.ToInt32(cardDataDict["power"]);
                }

                if (sender == "player")
                {
                    if (NumPlayerHand < 5)
                    {
                        var card = Instantiate(Card, new Vector3(0, 0, 0), Quaternion.identity);
                        card.GetComponent<CardScript>().Owner = sender;
                        card.GetComponent<CardScript>().Type = type;
                        card.transform.SetParent(PlayerArea.transform, false);
                        card.GetComponent<CardScript>().Name = name;
                        if (type == "creature")
                        {
                            card.GetComponent<CardScript>().Class = cclass;
                            card.GetComponent<CardScript>().Race = race;
                            card.GetComponent<CardScript>().level = level;
                            card.GetComponent<CardScript>().hp = hp;
                            card.GetComponent<CardScript>().ac = ac;
                            card.GetComponent<CardScript>().damage = damage;
                            card.GetComponent<CardScript>().magic = magic;
                            card.GetComponent<CardScript>().range = range;
                        }
                        else
                        {
                            card.GetComponent<CardScript>().power = power;
                        }

                        Dictionary<string, object> cardData = new()
                        {
                            ["Type"] = type,
                            ["Name"] = name
                        };

                        if (type == "creature")
                        {

                            cardData.Add("cclass", cclass);
                            cardData.Add("race", race);
                            cardData.Add("level", level);
                            cardData.Add("hp", hp);
                            cardData.Add("ac", type);
                            cardData.Add("damage", type);
                            cardData.Add("magic", type);
                            cardData.Add("range", type);
                        }
                        else
                        {
                            cardData.Add("power", power);
                        }

                        PlayerHand.Add(cardData);

                        NumPlayerHand++;
                    }
                    else
                    {
                        Dictionary<string, object> cardData = new()
                        {
                            ["Type"] = type,
                            ["Name"] = name
                        };
                        
                        if (type == "creature")
                        {
                            
                            cardData.Add("cclass", cclass);
                            cardData.Add("race", race);
                            cardData.Add("level", level);
                            cardData.Add("hp", hp);
                            cardData.Add("ac", type);
                            cardData.Add("damage", type);
                            cardData.Add("magic", type);
                            cardData.Add("range", type);
                        }
                        else
                        {
                            cardData.Add("power", power);
                        }

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
                        card.GetComponent<CardScript>().Type = type;
                        if (type == "creature")
                        {
                            card.GetComponent<CardScript>().Class = cclass;
                            card.GetComponent<CardScript>().Race = race;
                            card.GetComponent<CardScript>().level = level;
                            card.GetComponent<CardScript>().hp = hp;
                            card.GetComponent<CardScript>().ac = ac;
                            card.GetComponent<CardScript>().damage = damage;
                            card.GetComponent<CardScript>().magic = magic;
                            card.GetComponent<CardScript>().range = range;
                        }
                        else
                        {
                            card.GetComponent<CardScript>().power = power;
                        }

                        Dictionary<string, object> cardData = new()
                        {
                            ["Type"] = type,
                            ["Name"] = name
                        };

                        if (type == "creature")
                        {

                            cardData.Add("cclass", cclass);
                            cardData.Add("race", race);
                            cardData.Add("level", level);
                            cardData.Add("hp", hp);
                            cardData.Add("ac", type);
                            cardData.Add("damage", type);
                            cardData.Add("magic", type);
                            cardData.Add("range", type);
                        }
                        else
                        {
                            cardData.Add("power", power);
                        }

                        EnemyHand.Add(cardData);

                        NumEnemyHand++;
                    }
                    else
                    {
                        Dictionary<string, object> cardData = new()
                        {
                            ["Type"] = type,
                            ["Name"] = name
                        };

                        if (type == "creature")
                        {

                            cardData.Add("cclass", cclass);
                            cardData.Add("race", race);
                            cardData.Add("level", level);
                            cardData.Add("hp", hp);
                            cardData.Add("ac", type);
                            cardData.Add("damage", type);
                            cardData.Add("magic", type);
                            cardData.Add("range", type);
                        }
                        else
                        {
                            cardData.Add("power", power);
                        }

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

    private void EnemyPlayCardsAction()
    {
        ActionsQueue.Enqueue(() =>
        {
            if (EnemyPlayCards)
            {
                var random = new System.Random();

                foreach (var cardData in EnemyHand)
                {
                    #region CREAR CELDA

                    GameObject cell = null;
                    var randomCellName = "";

                    var trying = true;
                    while (trying)
                    {
                        randomCellName = "(" + random.Next(3) + ", " + random.Next(6) + ")";

                        cell = GameObject.Find(randomCellName);

                        if (cell == null)
                        {
                            UnityEngine.Debug.LogError("Cell " + randomCellName + " not found.");
                            return;
                        }

                        if (cell.transform.childCount > 0)
                        {
                            continue;
                        }
                        trying = false;
                    }

                    #endregion

                    if (cardData.TryGetValue("level", out object manaCostObj) && manaCostObj is int manaCost)
                    {
                        if (CurrentEnemyManaPool >= manaCost)
                        {
                            if (cardData.TryGetValue("Name", out object cardName) && cardName is string name)
                            {
                                var cardObject = GameObject.Find(name);
                                cardObject.transform.SetParent(cell.transform, false);
                                var cardScript = cardObject.GetComponent<CardScript>();

                                #region CARD DICT
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
                                var newCard = card;
                                newCard.Add("pos", cell.transform.name);

                                cardScript.transform.SetParent(cell.transform);
                                EnemyDeck.Remove(card);
                                NumEnemyHand--;
                                EnemyCardsInTable.Add(newCard);
                                NumEnemyCardsInTable++;
                                CurrentEnemyManaPool -= manaCost;

                                CardsInTable.Add(newCard);
                                #endregion
                            }
                        }
                    }
                }

                EnemyHand = EnemyHand.Except(EnemyCardsInTable).ToList();

                foreach (var enemyCardInTable in EnemyCardsInTable)
                {
                    Dictionary<string, object> cardData = new()
                    {
                        ["action"] = "createEnemyCard",
                        ["data"] = enemyCardInTable["Name"],
                        ["pos"] = enemyCardInTable["pos"]
                    };
                    var createEnemyCardActionJson = JsonConvert.SerializeObject(cardData, Formatting.Indented);
                    SendMessages(createEnemyCardActionJson);
                }

                SendMessages("enemyReady");

                EnemyManaPool++;
                CurrentEnemyManaPool = EnemyManaPool;
            }
        });
    }

    private void CardsToArea()
    {
        ActionsQueue.Enqueue(() =>
        {
            foreach (var card in CardsInTable)
            {
                if (card.TryGetValue("Name", out object cardName) && cardName is string name)
                {
                    var cardObject = GameObject.Find(name);
                    var cardScript = cardObject.GetComponent<CardScript>();

                    if (card.TryGetValue("Owner", out object cardOwner) && cardOwner is string owner)
                    {
                        if (owner == "player" && PlayerPlayCards)
                        {
                            cardObject.transform.SetParent(PlayerArea.transform, false);
                            cardScript.transform.SetParent(PlayerArea.transform, false);

                            PlayerDeck.Add(card);
                            NumPlayerHand++;
                            PlayerCardsInTable.Remove(card);
                            NumPlayerCardsInTable--;
                        }
                        else
                        {
                            if (owner == "enemy" && EnemyPlayCards)
                            {
                                cardObject.transform.SetParent(EnemyArea.transform, false);
                                cardScript.transform.SetParent(EnemyArea.transform, false);

                                EnemyDeck.Add(card);
                                NumEnemyHand++;
                                EnemyCardsInTable.Remove(card);
                                NumEnemyCardsInTable--;
                            }
                        }
                    }

                }
            }

            CardsInTable = CardsInTable.Except(PlayerDeck).ToList();
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

