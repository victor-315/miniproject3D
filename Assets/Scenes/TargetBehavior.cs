using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    private GridshotSpawner spawner;
    private Vector3 myPosition;

    public void Init(GridshotSpawner spawnerRef, Vector3 pos)
    {
        spawner = spawnerRef;
        myPosition = pos;
    }

    void OnMouseDown()
    {
        if (spawner != null)
        {
            spawner.OnTargetDestroyed(myPosition);
        }

        Destroy(gameObject);
    }
}