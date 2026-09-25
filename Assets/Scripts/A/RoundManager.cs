using System;
using UnityEngine;

// 一局的流程：倒计时、开始、结束判定、提前撤离、重开。场景里放一个。
// 第 1 步只有外壳：状态固定为 Playing，方便在补全前测试移动；第 5 步补全。
public class RoundManager : MonoBehaviour
{
    public enum RoundState { Ready, Playing, Ended }

    public static RoundManager Instance { get; private set; }

    public RoundState State { get; private set; } = RoundState.Playing;
    public float TimeRemaining { get; private set; }
    public bool Extracted { get; private set; }
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
