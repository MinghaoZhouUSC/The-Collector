using System;
using UnityEngine;

// 一局的流程：倒计时、开始、通关判定、提前结束、重开。场景里放一个。
// 第 1 步只有外壳：状态固定为 Playing，方便在补全前测试移动；第 5 步补全。
public class RoundManager : MonoBehaviour
{
    public enum RoundState { Ready, Playing, Ended }

    [Tooltip("通关目标：结束时已存价值要达到这个数")]
    [SerializeField, Min(0)] private int targetValue = 500;

    public static RoundManager Instance { get; private set; }

    public RoundState State { get; private set; } = RoundState.Playing;
    public float TimeRemaining { get; private set; }
    public int TargetValue => targetValue;
    // 结束时人是否在安全屋里。
    public bool Extracted { get; private set; }
    // 通关：Extracted 并且已存价值 ≥ TargetValue。
    public bool Won { get; private set; }
    // 结束时的已存价值；不在安全屋里为 0。
    public int FinalScore { get; private set; }

    public event Action StateChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void SetState(RoundState state)
    {
        State = state;
        StateChanged?.Invoke();
    }
}
