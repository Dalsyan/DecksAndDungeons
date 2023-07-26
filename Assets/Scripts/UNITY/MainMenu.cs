using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Build.Content;
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

    // Inputs
    public TMP_InputField LoginInput;
    public TMP_InputField LoginPasswordInput;
    public TMP_InputField RegisterInput;
    public TMP_InputField RegisterPasswordInput;
    public TMP_InputField RegisterVerifyPasswordInput;

    // Gestion de cuentas
    public string LoginText;
    public string LoginPasswordTexe;
    public string RegisterText;
    public string RegisterPasswordText;
    public string RegisterVerifyPasswordText;

    public GameObject DeckServer;
    private DeckManager DeckManager;

    public bool Logged {  get; set; }
    public Dictionary<string, string> User { get; set; }

    private void Start()
    {
        DeckManager = DeckServer.GetComponent<DeckManager>();
        Logged = false;
        User = new Dictionary<string, string>();
    }

    public void PlayGame()
    {
        DeckManager.Instance.SendMessages("close");
        Destroy(DeckManager.Instance);

        if (Logged)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            Debug.Log("No estas loggeado");
        }
    }

    public void CollectionButton()
    {
        CollectionMenu.SetActive(true);
        LoginMenu.SetActive(false);
    }

    public void OptionsButton()
    {
        OptionsMenu.SetActive(true);
        LoginMenu.SetActive(false);
    }

    public void BackButton()
    {
        OptionsMenu.SetActive(false);
        CollectionMenu.SetActive(false);
        RegisterMenu.SetActive(false);
        LoginMenu.SetActive(true);
    }
    
    public void LoginButton()
    {
        LoginMenu.SetActive(false);
        Logged = true;
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

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}
