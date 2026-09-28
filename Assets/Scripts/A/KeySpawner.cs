using UnityEngine;

// 钥匙刷新器：子物体是候选点。开局按概率在其中一个点刷出全图唯一的一把钥匙。
public class KeySpawner : MonoBehaviour
{
    [Tooltip("钥匙的 ItemDefinition（Is Key 要勾选）")]
    [SerializeField] private ItemDefinition key;
    [Tooltip("开局出现钥匙的概率")]
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.7f;

    private void Start()
    {
        if (key == null || key.data == null)
        {
            Debug.LogWarning("[KeySpawner] 没有设置钥匙的 ItemDefinition。", this);
            return;
        }

        if (!key.data.isKey)
            Debug.LogWarning($"[KeySpawner] {key.name} 没有勾选 Is Key，会被当成普通物品。", this);

        if (transform.childCount == 0 || Random.value >= spawnChance) return;

        Transform point = transform.GetChild(Random.Range(0, transform.childCount));
        ItemFactory.Spawn(key.data.Clone(), point.position);
    }

    // 在 Scene 视图里用绿框标出候选点。
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 0.5f, 0.9f);
        foreach (Transform child in transform)
            Gizmos.DrawWireCube(child.position, Vector3.one * 0.5f);
    }
}
