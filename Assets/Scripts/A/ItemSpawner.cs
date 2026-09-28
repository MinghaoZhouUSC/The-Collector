using System.Collections.Generic;
using UnityEngine;

// 物品刷新器：子物体就是刷新点。开局把刷新点打乱，从物品池里抽 count 件，同一个点不会刷两件。
// 每个房间放一个，各自配置物品池（越远的房间越值钱）。
public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private ItemPool pool = new ItemPool();
    [Tooltip("开局刷几件。刷新点比这个数少时，只刷满所有刷新点")]
    [SerializeField, Min(0)] private int count = 7;

    private void Start()
    {
        var points = new List<Transform>();
        foreach (Transform child in transform) points.Add(child);

        if (count > points.Count)
            Debug.LogWarning($"[ItemSpawner] {name} 只有 {points.Count} 个刷新点，少于要刷的 {count} 件。", this);

        Shuffle(points);
        int n = Mathf.Min(count, points.Count);
        for (int i = 0; i < n; i++)
        {
            ItemData item = pool.PickRandom();
            if (item != null) ItemFactory.Spawn(item, points[i].position);
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 在 Scene 视图里用黄圈标出刷新点，方便摆放。
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        foreach (Transform child in transform)
            Gizmos.DrawWireSphere(child.position, 0.3f);
    }
}
