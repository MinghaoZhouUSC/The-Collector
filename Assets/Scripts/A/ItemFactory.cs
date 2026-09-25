using System.Collections.Generic;
using UnityEngine;

// 生成地上的物品。场景里放一个。
// 第 1 步只有外壳：调用不会报错，但还不会生成物品（第 3 步补全）。
public class ItemFactory : MonoBehaviour
{
    public static WorldItem Spawn(ItemData item, Vector2 position)
    {
        Debug.LogWarning("[ItemFactory] 还没实现（A 第 3 步），这次不会生成物品。");
        return null;
    }

    // 把多件物品散落在 center 周围，自动避开墙和其他实心物体。
    public static void Burst(IEnumerable<ItemData> items, Vector2 center)
    {
        Debug.LogWarning("[ItemFactory] 还没实现（A 第 3 步），这次不会生成物品。");
    }
}
