using UnityEngine;

// 生成地上的物品。场景里放一个，并在 Inspector 里设置 World Item Prefab。
public class ItemFactory : MonoBehaviour
{
    [SerializeField] private WorldItem worldItemPrefab;

    private static ItemFactory instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[ItemFactory] 场景里有多个 ItemFactory，只会使用第一个。");
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // 在 position 生成一件物品。
    public static WorldItem Spawn(ItemData item, Vector2 position)
    {
        if (item == null || !IsReady()) return null;

        WorldItem worldItem = Instantiate(instance.worldItemPrefab, position, Quaternion.identity, instance.transform);
        worldItem.Init(item);
        return worldItem;
    }

    private static bool IsReady()
    {
        if (instance != null && instance.worldItemPrefab != null) return true;

        Debug.LogWarning("[ItemFactory] 场景里需要一个 ItemFactory，并设置好 World Item Prefab。");
        return false;
    }
}
