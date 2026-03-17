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

    private List<Vector3> allPositions = new List<Vector3>();
    private List<Vector3> occupiedPositions = new List<Vector3>();

    void Start()
    {
        GenerateGridPositions();
        SpawnInitialTargets();
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
        if (allPositions.Count == occupiedPositions.Count) return;

        Vector3 spawnPos;

        // Find a free position
        do
        {
            spawnPos = allPositions[Random.Range(0, allPositions.Count)];
        }
        while (occupiedPositions.Contains(spawnPos));

        GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity, transform);

        // Random size
        float size = sizes[Random.Range(0, sizes.Length)];
        target.transform.localScale = Vector3.one * size;

        // Track position
        occupiedPositions.Add(spawnPos);

        // Init behavior
        TargetBehavior tb = target.GetComponent<TargetBehavior>();
        if (tb != null)
        {
            tb.Init(this, spawnPos);
        }
    }

    public void OnTargetDestroyed(Vector3 position)
    {
        occupiedPositions.Remove(position);

        // Spawn a new one to keep count at 3
        SpawnRandomTarget();
    }
}