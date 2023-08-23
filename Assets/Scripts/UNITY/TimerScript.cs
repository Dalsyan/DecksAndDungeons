using System.Collections;
using TMPro;
using UnityEngine;

public class TimerScript : MonoBehaviour
{
    public float CountdownTime = 90f;
    public TextMeshProUGUI CounterText;

    private float currentTime;

    private void Start()
    {
        currentTime = CountdownTime;
        UpdateCountdownText();
        InvokeRepeating("UpdateTimer", 1f, 1f);
    }

    private void UpdateTimer()
    {
        currentTime -= 1f;
        UpdateCountdownText();

        if (currentTime <= 0f)
        {
            CancelInvoke("UpdateTimer");
        }
    }

    private void UpdateCountdownText()
    {
        CounterText.text = currentTime.ToString("F0");
    }

    public void RestartCounter()
    {
        currentTime = 90f;
    }
}
