using Mono.Cecil;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // Menus
    public GameObject OptionsMenu;
    public GameObject CollectionMenu;
    public GameObject LoginMenu;
    public GameObject RegisterMenu;
    public GameObject UserMenu;

    // Backgrounds
    public GameObject DeckBackground;
    public GameObject CardBackground;
    public GameObject CardFromDeckBackground;

    // Inputs
    public TMP_InputField LoginInput;
    public TMP_InputField LoginPasswordInput;
    public TMP_InputField RegisterInput;
    public TMP_InputField RegisterPasswordInput;
    public TMP_InputField RegisterVerifyPasswordInput;

    // Gestion de cuentas
    private string LoginText;
    private string LoginPasswordText;
    private string RegisterText;
    private string RegisterPasswordText;
    private string RegisterVerifyPasswordText;
    private string CurrentSelectedDeck;
    private string SelectedDeck;
    private int Wins;
    private int Loses;

    // DeckManager
    public GameObject DeckServer;
    private DeckManager DeckManager;

    // Botones
    public GameObject DeckButton;
    public List<string> DeckButtonList;
    public GameObject CardButton;
    public List<string> CardButtonList;
    public List<string> CardDeckButtonList;
    public GameObject SelectDeckButton;

    // Cartas
    public GameObject CardRedux;

    // Paginacion
    public GameObject RightPage;
    public GameObject LeftPage;
    public int CurrentPage = 1;

    private void Start()
    {
        DeckManager = DeckServer.GetComponent<DeckManager>();

        if (PlayerPrefs.HasKey("LoginText"))
        {
            LoginText = PlayerPrefs.GetString("LoginText");
            Debug.Log(LoginText);

            if (!string.IsNullOrEmpty(LoginText))
            {
                LoginMenu.SetActive(false);

                DeckManager.Instance.Logged = true;
                UserMenu.SetActive(true);

                var usernameText = UserMenu.GetComponentInChildren<TMP_Text>();
                usernameText.text = LoginText;
            }
            else
            {
                LoginMenu.SetActive(true);
            }
        }

        if (PlayerPrefs.HasKey("SelectedDeck"))
        {
            SelectedDeck = PlayerPrefs.GetString("SelectedDeck");
            Debug.Log(SelectedDeck);

            if (!string.IsNullOrEmpty(SelectedDeck))
            {
                DeckManager.Instance.SelectedDeck = true;

                var selectedDeckText = UserMenu.transform.Find("SelectedDeckText").GetComponentInChildren<TMP_Text>();
                selectedDeckText.text = SelectedDeck;
            }
        }

        if (PlayerPrefs.HasKey("Wins"))
        {
            var nWins = PlayerPrefs.GetInt("Wins");
            Debug.Log(nWins);

            if (!string.IsNullOrEmpty(nWins.ToString()))
            {
                var nWinsText = UserMenu.transform.Find("Wins").GetComponentInChildren<TMP_Text>();
                nWinsText.text = nWins.ToString();
            }
        }

        if (PlayerPrefs.HasKey("Loses"))
        {
            var nLoses = PlayerPrefs.GetInt("Loses");
            Debug.Log(nLoses);

            if (!string.IsNullOrEmpty(nLoses.ToString()))
            {
                var nLosesText = UserMenu.transform.Find("Loses").GetComponentInChildren<TMP_Text>();
                nLosesText.text = nLoses.ToString();
            }
        }
    }

    public void PlayGame()
    {
        if (DeckManager.Instance.Logged)
        {
            if (DeckManager.Instance.SelectedDeck)
            {
                DeckManager.Instance.SendMessages("close");
                // Destroy(DeckManager.Instance);

                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
            else
            {
                Debug.Log("Necesitas seleccionar un mazo!");
            }
        }
        else
        {
            Debug.Log("No estas loggeado");
        }
    }

    #region FORMS

    public void CollectionButton()
    {
        if (DeckManager.Instance.Logged)
        {
            CollectionMenu.SetActive(true);
            LoginMenu.SetActive(false);
        }
        else
        {
            Debug.Log("No estas loggeado");
        }
    }

    public void OptionsButton()
    {
        OptionsMenu.SetActive(true);
        LoginMenu.SetActive(false);
    }

    #endregion

    #region USER

    public void LoginButton()
    {
        LoginText = LoginInput.text;
        LoginPasswordText = LoginPasswordInput.text;

        LoginMessage();
    }
    
    public void RegisterMenuButton()
    {
        RegisterMenu.SetActive(true);
        LoginMenu.SetActive(false);
    }

    public void RegisterButton()
    {
        RegisterText = RegisterInput.text;
        RegisterPasswordText = RegisterPasswordInput.text;
        RegisterVerifyPasswordText = RegisterVerifyPasswordInput.text;


        if (RegisterPasswordText == RegisterVerifyPasswordText)
        {
            Dictionary<string, object> userData = new()
            {
                ["action"] = "registerUser",
                ["data"] = RegisterText,
                ["password"] = RegisterPasswordText
            };

            var createUserJson = JsonConvert.SerializeObject(userData, Formatting.Indented);
            DeckManager.Instance.SendMessages(createUserJson);

            LoginText = RegisterInput.text;
            LoginPasswordText = RegisterPasswordInput.text;
            LoginMessage();

            RegisterMenu.SetActive(false);
        }
        else
        {
            Debug.Log("Passwords doesnt match, try again");
        }
    }

    public void LogOutButton()
    {
        LoginText = null;
        SelectedDeck = null;
        Wins = 0;
        Loses = 0;
        PlayerPrefs.SetString("LoginText", LoginText);
        PlayerPrefs.SetString("SelectedDeck", SelectedDeck);
        PlayerPrefs.SetInt("Wins", Wins);
        PlayerPrefs.SetInt("Loses", Loses);
        DeckManager.Instance.Logged = false;

        UserMenu.SetActive(false);
        CollectionMenu.SetActive(false);
        LoginMenu.SetActive(true);
    }

    private void LoginMessage()
    {
        Dictionary<string, object> userData = new()
        {
            ["action"] = "loginUser",
            ["data"] = LoginText,
            ["password"] = LoginPasswordText
        };

        var loginJson = JsonConvert.SerializeObject(userData, Formatting.Indented);
        DeckManager.Instance.SendMessages(loginJson);

        if (DeckManager.Instance.Logged)
        {
            PlayerPrefs.SetString("LoginText", LoginText);

            var usernameText = UserMenu.GetComponentInChildren<TMP_Text>();
            usernameText.text = LoginText;

            LoginMenu.SetActive(false);
            UserMenu.SetActive(true);
        }
        else
        {
            Debug.Log("Incorrect user data");
        }
    }
    
    #endregion

    #region COLLECTION MANAGMENT

    public void CreateDeck()
    {
        var createDeckDict = new Dictionary<string, string>()
        {
            ["action"] = "createDeck",
            ["data"] = LoginText
        };

        var createDeckDictJson = JsonConvert.SerializeObject(createDeckDict, Formatting.Indented);
        DeckManager.Instance.SendMessages(createDeckDictJson);
    }

    public void ShowDecksButton()
    {
        CardBackground.SetActive(false);
        DeckBackground.SetActive(true);
        SelectDeckButton.SetActive(true);

        RightPage.SetActive(false);
        LeftPage.SetActive(false);

        var deckDict = new Dictionary<string, string>()
        {
            ["action"] = "showDecks",
            ["data"] = LoginText
        };

        var deckDictJson = JsonConvert.SerializeObject(deckDict, Formatting.Indented);
        DeckManager.Instance.SendMessages(deckDictJson);

        foreach (string deck in DeckManager.Instance.DeckNames)
        {
            if (DeckButtonList.Any(x => x == deck))
            {
                continue;
            }

            DeckButtonList.Add(deck);

            var deckButton = Instantiate(DeckButton, new Vector3(0, 0, 0), Quaternion.identity);
            deckButton.name = deck;

            var buttonComp = deckButton.GetComponent<Button>();
            buttonComp.onClick.AddListener(() => SelectDeck(deck));

            var deckButtonText = deckButton.GetComponentInChildren<TextMeshProUGUI>();
            deckButtonText.text = deck;

            deckButton.transform.SetParent(DeckBackground.transform, false);
        }
    }

    public void SelectDeck(string deck)
    {
        CardFromDeckBackground.SetActive(true);
        SelectDeckButton.SetActive(true);

        ShowCardsMessage(deck);

        var buttonComp = SelectDeckButton.GetComponent<Button>();
        buttonComp.onClick.AddListener(() => SelectCurrentDeckButtonEvent(deck));
    }

    public void SelectCurrentDeckButtonEvent(string deck)
    {
        CurrentSelectedDeck = deck;
        SelectDeckButtonEvent();
    }

    public void SelectDeckButtonEvent()
    {
        PlayerPrefs.SetString("SelectedDeck", CurrentSelectedDeck);

        if (PlayerPrefs.HasKey("SelectedDeck"))
        {
            SelectedDeck = PlayerPrefs.GetString("SelectedDeck");
            Debug.Log(SelectedDeck);

            if (!string.IsNullOrEmpty(SelectedDeck))
            {
                DeckManager.Instance.SelectedDeck = true;

                var selectedDeckText = UserMenu.transform.Find("SelectedDeckText").GetComponentInChildren<TMP_Text>();
                selectedDeckText.text = SelectedDeck;
            }
        }
    }

    public void ShowCardsButton()
    {
        DeckBackground.SetActive(false);
        CardFromDeckBackground.SetActive(false);
        CardBackground.SetActive(true);

        RightPage.SetActive(true);

        ShowCardsMessage();
    }

    private void ShowCardsMessage(string deck = null)
    {
        var cardDict = new Dictionary<string, object>();

        if (string.IsNullOrEmpty(deck))
        {
            cardDict.Add("action", "showCards");
            cardDict.Add("data", LoginText);
            cardDict.Add("page", CurrentPage);
        }
        else
        {
            cardDict.Add("action", "showDeckCards");
            cardDict.Add("data", deck);
        }

        var cardDictJson = JsonConvert.SerializeObject(cardDict, Formatting.Indented);
        DeckManager.Instance.SendMessages(cardDictJson);
    }

    public void SetNextPage()
    {
        LeftPage.SetActive(true);

        foreach (string card in DeckManager.Instance.CardNames)
        {
            Destroy(GameObject.Find(card));
        }

        DeckManager.Instance.CardNames.Clear();

        CurrentPage++;

        if (DeckManager.Instance.CardNames.Count < 8)
        {

            RightPage.SetActive(false);
        }

        ShowCardsMessage();
    }

    public void SetPreviousPage()
    {
        RightPage.SetActive(true);

        foreach (string card in DeckManager.Instance.CardNames)
        {
            Destroy(GameObject.Find(card));
        }

        DeckManager.Instance.CardNames.Clear();

        CurrentPage--;

        if (CurrentPage <= 1)
        {
            LeftPage.SetActive(false);
        }

        ShowCardsMessage();
    }

    #endregion

    public void BackButton()
    {
        OptionsMenu.SetActive(false);
        CollectionMenu.SetActive(false);
        RegisterMenu.SetActive(false);

        DeckBackground.SetActive(false);
        CardBackground.SetActive(false);
        CardFromDeckBackground.SetActive(false);

        CurrentPage = 1;
        RightPage.SetActive(false);
        LeftPage.SetActive(false);

        if (!DeckManager.Instance.Logged)
        {
            LoginMenu.SetActive(true);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");

        DeckManager.Instance.SendMessages("close");
        Application.Quit();
    }
}
