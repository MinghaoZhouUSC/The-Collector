using UnityEngine;

// 每种物品一个资源文件（Low / Mid / High / Gem / Key），数值只在这里维护一份。
// 创建方法：Project 窗口右键 → Create → The Collector → Item Definition。
[CreateAssetMenu(fileName = "Item_", menuName = "The Collector/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public ItemData data = new ItemData();
}
