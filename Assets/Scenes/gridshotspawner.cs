using UnityEngine;
using System.Collections.Generic;

public class GridshotSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 4;
    public int columns = 4;
    public float spacing = 2f;

    [Header("Target Settings")]
    public GameObject targetPrefab;
    public float[] sizes = { 0.5f, 0.75f, 1f, 1.5f };

    [Header("Gameplay")]
    public int maxActiveTargets = 3;
    public float gameDuration = 30f;

    private List<Vector3> allPositions = new List<Vector3>();
    private List<Vector3> occupiedPositions = new List<Vector3>();

    // Score system
    private int score = 0;
    private int shotsFired = 0;
    private int shotsHit = 0;

    private float timer;
    private bool gameActive = true;

    void Start()
    {
        timer = gameDuration;
        GenerateGridPositions();
        SpawnInitialTargets();
    }

    void Update()
    {
        // ALWAYS allow reset (even after game over)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
            return; // stop this frame to avoid conflicts
        }

        if (!gameActive) return;

        // TIMER
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            gameActive = false;

            Debug.Log("Game Over!");
            Debug.Log("Final Score: " + score);
            Debug.Log("Accuracy: " + GetAccuracy().ToString("F1") + "%");

            return;
        }

        // SHOOTING
        if (Input.GetMouseButtonDown(0) or Input.GetKeyDown(KeyCode.X))
        {
            shotsFired++;
            
        }
    }

    void GenerateGridPositions()
    {
        Vector3 startPos = transform.position;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 pos = startPos + new Vector3(c * spacing, r * spacing, 0);
                allPositions.Add(pos);
            }
        }
    }

    void SpawnInitialTargets()
    {
        for (int i = 0; i < maxActiveTargets; i++)
        {
            SpawnRandomTarget();
        }
    }

    void SpawnRandomTarget()
    {
        if (!gameActive) return;
        if (allPositions.Count == occupiedPositions.Count) return;

        Vector3 spawnPos;

        do
        {
            spawnPos = allPositions[Random.Range(0, allPositions.Count)];
        }
        while (occupiedPositions.Contains(spawnPos));

        GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity, transform);

        float size = sizes[Random.Range(0, sizes.Length)];
        target.transform.localScale = Vector3.one * size;

        occupiedPositions.Add(spawnPos);

        TargetBehavior tb = target.GetComponent<TargetBehavior>();
        if (tb != null)
        {
            tb.Init(this, spawnPos);
        }
    }
    
    public void OnTargetHit(Vector3 position)
    {
        if (!gameActive) return;

        score++;
        shotsHit++;

        occupiedPositions.Remove(position);
        SpawnRandomTarget();
    }

    void EndGame()
    {
        Debug.Log("Game Over!");
        Debug.Log("Score: " + score);

        float accuracy = shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
        Debug.Log("Accuracy: " + accuracy.ToString("F1") + "%");
    }

    // UI getters
    public int GetScore() => score;

    public float GetAccuracy()
    {
        return shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
    }

    public float GetTime() => timer;
    public void ResetGame()
    {
        // Reset stats
        score = 0;
        shotsFired = 0;
        shotsHit = 0;

        // Reset timer
        timer = gameDuration;
        gameActive = true;

        // Destroy all current targets
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Clear occupied positions
        occupiedPositions.Clear();

        // Respawn targets
        SpawnInitialTargets();

        Debug.Log("Game Reset!");
    }
}