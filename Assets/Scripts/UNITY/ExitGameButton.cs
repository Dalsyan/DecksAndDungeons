using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitGameButton : MonoBehaviour
{
    public GameObject ServerSockets;
    public void ExitGame()
    {
        Destroy(ServerSockets);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void RecvCardFromDeck()
    {

    }
}
