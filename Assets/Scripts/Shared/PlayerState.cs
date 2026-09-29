using UnityEngine;

// 玩家的共享状态。A 创建最小版，B 负责扩展（例如 Freeze）。
// A 的代码只读取这两个属性，从不写入；B 可以改实现方式，但不要改名。
public class PlayerState : MonoBehaviour
{
    [Tooltip("为 true 时玩家不能移动，也不能按 E/Q。测试时可以在 Inspector 里手动勾选。")]
    [SerializeField] private bool isFrozen;

    [Tooltip("玩家是否在安全屋区域内，由 B 的 SafeHouseZone 设置。测试时可以在 Inspector 里手动勾选。")]
    [SerializeField] private bool isInSafeHouse;

    private float frozenUntil = float.NegativeInfinity;

    public void Freeze(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;
        frozenUntil = Mathf.Max(frozenUntil, Time.time + seconds);
    }

    public bool IsFrozen
    {
        get => isFrozen || Time.time < frozenUntil;
        set => isFrozen = value;
    }

    public bool IsInSafeHouse
    {
        get => isInSafeHouse;
        set => isInSafeHouse = value;
    }
}
