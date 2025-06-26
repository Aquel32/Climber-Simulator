using UnityEngine;

public class Camp : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform tentPrefab;

    private Vector2Int point;
    private int realSize;

    public void Initialize(int campSize, int campNumber, int campsCount)
    {
        transform.name = "Camp #" + campNumber;

        point = TerrainMapGenerator.Instance.ConvertWorldPositionToPoint(transform.position);
        realSize = (int)(((campsCount - campNumber) / (float)campsCount) * campSize);

        for (int i = 0; i < realSize; i++)
        {
            int x = TerrainMapGenerator.Instance.random.Next(-realSize, realSize);
            int y = TerrainMapGenerator.Instance.random.Next(-realSize, realSize);

            Vector2Int newPoint = new Vector2Int(point.x + x, point.y + y);
            Vector3 tentPosition = TerrainMapGenerator.Instance.ConvertPointToWorldPosition(newPoint);

            Instantiate(tentPrefab, tentPosition, Quaternion.identity, transform);
        }
    }
}
