using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GridshotSpawner spawner;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI timerText;

    void Update()
    {
        scoreText.text = "Score: " + spawner.GetScore();
        accuracyText.text = "Accuracy: " + spawner.GetAccuracy().ToString("F1") + "%";
        timerText.text = "Time: " + spawner.GetTime().ToString("F1");
    }
}