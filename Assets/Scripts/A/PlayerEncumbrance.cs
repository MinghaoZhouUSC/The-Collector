using UnityEngine;

// 负重 → 速度。所有和"重量让人变慢"有关的调参都集中在这里。
[RequireComponent(typeof(PlayerInventory))]
public class PlayerEncumbrance : MonoBehaviour
{
    [Tooltip("空背包时的移动速度（单位/秒）")]
    [SerializeField, Min(0.1f)] private float baseSpeed = 5f;

    [Tooltip("背包装满时的速度倍率。0.3 = 满载时只有基础速度的 30%")]
    [SerializeField, Range(0.05f, 1f)] private float fullLoadSpeedMultiplier = 0.3f;

    [Tooltip("减速曲线。横轴：负重比例（0 = 空，1 = 满）；纵轴：减速程度（0 = 不减速，1 = 降到满载速度）。默认是直线。")]
    [SerializeField] private AnimationCurve slowdownCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("速度档位（按速度倍率划分）")]
    [Tooltip("速度倍率不低于这个值时显示 Normal")]
    [SerializeField, Range(0f, 1f)] private float normalThreshold = 0.8f;
    [Tooltip("速度倍率不低于这个值时显示 Slow，更低则显示 Very Slow")]
    [SerializeField, Range(0f, 1f)] private float slowThreshold = 0.5f;

    private PlayerInventory inventory;

    // 用到时再取，避免别的脚本在 Awake 顺序上先调用而拿到空引用。
    private PlayerInventory Inventory => inventory != null ? inventory : (inventory = GetComponent<PlayerInventory>());

    // 负重比例：0 = 空，1 = 满。
    public float LoadRatio => Mathf.Clamp01(Inventory.TotalWeight / Inventory.Capacity);

    public float SpeedMultiplier =>
        Mathf.Lerp(1f, fullLoadSpeedMultiplier, Mathf.Clamp01(slowdownCurve.Evaluate(LoadRatio)));

    public float CurrentSpeed => baseSpeed * SpeedMultiplier;

    public string SpeedLabel
    {
        get
        {
            float multiplier = SpeedMultiplier;
            if (multiplier >= normalThreshold) return "Normal";
            if (multiplier >= slowThreshold) return "Slow";
            return "Very Slow";
        }
    }
}
