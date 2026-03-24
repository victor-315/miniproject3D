using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI timerText;

    private int score = 0;
    private int shotsFired = 0;
    private int shotsHit = 0;

    private float timer;
    private bool gameActive = true;

    public float gameDuration = 30f;

    void Start()
    {
        ResetStats();
    }

    void Update()
    {
        if (!gameActive) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 0f;
            gameActive = false;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        accuracyText.text = "Accuracy: " + GetAccuracy().ToString("F1") + "%";
        timerText.text = "Time: " + timer.ToString("F1");
    }

    // 🔥 CALLED WHEN PLAYER SHOOTS
    public void RegisterShot()
    {
        if (!gameActive) return;
        shotsFired++;
    }

    // 🔥 CALLED WHEN TARGET IS HIT
    public void RegisterHit()
    {
        if (!gameActive) return;

        shotsHit++;
        score++;
    }

    public float GetAccuracy()
    {
        return shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
    }

    public void ResetStats()
    {
        score = 0;
        shotsFired = 0;
        shotsHit = 0;

        timer = gameDuration;
        gameActive = true;
    }

    public bool IsActive() => gameActive;
}