using UnityEngine;

// 一件物品的数据。字段是和 B 约定好的，不要改名或删除。
[System.Serializable]
public class ItemData
{
    public string itemName = "Item";
    [Min(0f)] public float weight = 1f;
    [Min(0)] public int value = 1;
    public Color color = Color.white;
    [Tooltip("物品在地上显示的大小（localScale）")]
    [Min(0.05f)] public float scale = 0.5f;
    public bool isKey;
    [Tooltip("物品图片（Unity 自带形状），地上和背包页面共用。为空时使用 WorldItem Prefab 自己的图片")]
    public Sprite icon;

    // 返回一份副本。运行时只使用副本，避免在 Play 模式下误改 ItemDefinition 资源里的数值。
    public ItemData Clone() => (ItemData)MemberwiseClone();
}
