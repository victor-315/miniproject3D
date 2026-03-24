using UnityEngine;
using System.Collections.Generic;

public class SphereTrackSystem : MonoBehaviour
{
    [Header("Spawner")]
    public GameObject targetPrefab;
    public int maxTargets = 3;
    public Vector3 areaSize = new Vector3(10, 5, 0);

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float changeDirTime = 1.5f;

    [Header("Game")]
    public float gameDuration = 30f;

    [Header("Target Layer")]
    public LayerMask targetLayer;

    private List<GameObject> activeTargets = new List<GameObject>();

    private int score = 0;
    private int shotsFired = 0;
    private int shotsHit = 0;

    private float timer;
    private bool gameActive = true;

    void Start()
    {
        ResetGame();
    }

    void Update()
    {
        // RESET
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
            return;
        }

        if (!gameActive) return;

        // TIMER
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            gameActive = false;

            Debug.Log("Game Over!");
            Debug.Log("Score: " + score);
            Debug.Log("Accuracy: " + GetAccuracy().ToString("F1") + "%");
            return;
        }

        // SHOOTING (Click OR X)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.X))
        {
            shotsFired++;
            HandleShot();
        }
    }

    // ----------------------------
    // SPAWNING
    // ----------------------------
    void SpawnTarget()
    {
        Vector3 pos = GetRandomPosition();

        GameObject t = Instantiate(targetPrefab, pos, Quaternion.identity, transform);
        activeTargets.Add(t);

        SphereMover mover = t.AddComponent<SphereMover>();
        mover.Init(this, moveSpeed, changeDirTime, transform.position, areaSize);
    }

    Vector3 GetRandomPosition()
    {
        Vector3 c = transform.position;

        return new Vector3(
            c.x + Random.Range(-areaSize.x / 2, areaSize.x / 2),
            c.y + Random.Range(-areaSize.y / 2, areaSize.y / 2),
            c.z
        );
    }

    void SpawnInitial()
    {
        for (int i = 0; i < maxTargets; i++)
            SpawnTarget();
    }

    // ----------------------------
    // HIT SYSTEM (ONLY PLACE SCORE CHANGES)
    // ----------------------------
    public void RegisterHit(GameObject target)
    {
        if (!gameActive) return;

        if (activeTargets.Contains(target))
        {
            shotsHit++;
            score++;

            activeTargets.Remove(target);
            SpawnTarget();
        }
    }

    void HandleShot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, targetLayer))
        {
            GameObject obj = hit.collider.gameObject;

            if (activeTargets.Contains(obj))
            {
                RegisterHit(obj);
                Destroy(obj);
            }
        }
    }

    // ----------------------------
    // RESET
    // ----------------------------
    public void ResetGame()
    {
        StopAllCoroutines();

        score = 0;
        shotsFired = 0;
        shotsHit = 0;

        timer = gameDuration;
        gameActive = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        activeTargets.Clear();

        SpawnInitial();

        Debug.Log("Reset!");
    }

    // ----------------------------
    // UI HELPERS
    // ----------------------------
    public int GetScore() => score;

    public float GetAccuracy()
    {
        return shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
    }

    public float GetTime() => timer;

    public bool IsActive() => gameActive;
}