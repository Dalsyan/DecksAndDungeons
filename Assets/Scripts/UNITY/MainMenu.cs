using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
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

    // DeckManager
    public GameObject DeckServer;
    private DeckManager DeckManager;

    // Botones
    public GameObject DeckButton;
    public List<string> DeckButtonList;
    public GameObject CardButton;
    public List<string> CardButtonList;

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
        }
    }

    public void PlayGame()
    {
        if (DeckManager.Instance.Logged)
        {
            DeckManager.Instance.SendMessages("close");
            Destroy(DeckManager.Instance);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
        }
        else
        {
            Debug.Log("Passwords doesnt match, try again");
        }
    }

    public void LogOutButton()
    {
        LoginText = null;
        PlayerPrefs.SetString("LoginText", LoginText);
        DeckManager.Instance.Logged = false;

        UserMenu.SetActive(false);
        LoginMenu.SetActive(true);
    }

    #endregion

    #region COLLECTION MANAGMENT

    public void ShowDecksButton()
    {
        CardBackground.SetActive(false);
        DeckBackground.SetActive(true);

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
            buttonComp.onClick.AddListener(() => ShowCardsFromDeckButton());

            var deckButtonText = deckButton.GetComponentInChildren<TextMeshProUGUI>();
            deckButtonText.text = deck;

            deckButton.transform.SetParent(DeckBackground.transform, false);
        }
    }
    
    public void ShowCardsButton()
    {
        DeckBackground.SetActive(false);
        CardFromDeckBackground.SetActive(false);
        CardBackground.SetActive(true);

        var cardDict = new Dictionary<string, string>()
        {
            ["action"] = "showCards",
            ["data"] = LoginText
        };

        var cardDictJson = JsonConvert.SerializeObject(cardDict, Formatting.Indented);
        DeckManager.Instance.SendMessages(cardDictJson);

        foreach (string card in DeckManager.Instance.CardNames)
        {
            if (CardButtonList.Any(x => x == card))
            {
                continue;
            }

            CardButtonList.Add(card);

            var cardButton = Instantiate(CardButton, new Vector3(0, 0, 0), Quaternion.identity);
            cardButton.name = card;

            var cardButtonText = cardButton.GetComponentInChildren<TextMeshProUGUI>();
            cardButtonText.text = card;

            cardButton.transform.SetParent(CardBackground.transform, false);
        }
    }
    
    public void ShowCardsFromDeckButton()
    {
        CardFromDeckBackground.SetActive(true);

        var cardDict = new Dictionary<string, string>()
        {
            ["action"] = "showCardsFromDeck",
            ["data"] = "deck_name"
        };

        //var cardDictJson = JsonConvert.SerializeObject(cardDict, Formatting.Indented);
        //DeckManager.Instance.SendMessages(cardDictJson);
    }

    #endregion

    public void BackButton()
    {
        OptionsMenu.SetActive(false);
        CollectionMenu.SetActive(false);
        RegisterMenu.SetActive(false);
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
