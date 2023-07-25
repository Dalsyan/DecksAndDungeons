using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject OptionsMenu;
    public GameObject CollectionMenu;
    public GameObject LoginMenu;
    public GameObject RegisterMenu;

    public bool Logged {  get; set; }
    public Dictionary<string, string> User { get; set; }

    private void Start()
    {
        Logged = false;
        User = new Dictionary<string, string>();
    }

    public void PlayGame()
    {
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

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}
