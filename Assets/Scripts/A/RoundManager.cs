using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 一局的流程：Space 开始 → 倒计时 → 结束判定 → R 重开。场景里放一个。
// 通关：结束时人在安全屋里，并且已存价值 ≥ 目标。
// 已存够目标后，在安全屋里按 F 可以提前结束（直接通关）。
public class RoundManager : MonoBehaviour
{
    public enum RoundState { Ready, Playing, Ended }

    [Tooltip("一局的时长（秒）")]
    [SerializeField, Min(5f)] private float roundSeconds = 120f;
    [Tooltip("通关目标：结束时已存价值要达到这个数")]
    [SerializeField, Min(0)] private int targetValue = 500;

    [Header("按键")]
    [SerializeField] private Key startKey = Key.Space;
    [SerializeField] private Key extractKey = Key.F;
    [SerializeField] private Key restartKey = Key.R;

    private PlayerState player;
    private PlayerInventory inventory;

    public static RoundManager Instance { get; private set; }

    public RoundState State { get; private set; } = RoundState.Ready;
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
        TimeRemaining = roundSeconds;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;

        switch (State)
        {
            case RoundState.Ready:
                if (Pressed(kb, startKey)) StartRound();
                break;

            case RoundState.Playing:
                TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
                if (TimeRemaining <= 0f) EndRound();
                else if (Pressed(kb, extractKey)) TryExtractEarly();
                break;

            case RoundState.Ended:
                if (Pressed(kb, restartKey)) Restart();
                break;
        }
    }

    // Shared by the keyboard shortcut and the level-selection button.
    public void StartRound()
    {
        if (State == RoundState.Ready) SetState(RoundState.Playing);
    }

    public void ReturnToLevelSelect()
    {
        if (State == RoundState.Ended) Restart();
    }

    private void TryExtractEarly()
    {
        FindPlayer();
        if (player == null || inventory == null || !player.IsInSafeHouse)
        {
            GameHUD.ShowMessage("Get to the safe house to extract");
            return;
        }

        // 人在安全屋里时背包会被自动存入；这里再存一次，保证刚捡的东西也算上。
        inventory.DepositAll();
        int missing = targetValue - inventory.BankedValue;
        if (missing > 0)
        {
            GameHUD.ShowMessage($"Need ${missing} more to extract");
            return;
        }

        EndRound();
    }

    private void EndRound()
    {
        FindPlayer();
        Extracted = player != null && inventory != null && player.IsInSafeHouse;
        if (Extracted) inventory.DepositAll();

        FinalScore = Extracted ? inventory.BankedValue : 0;
        Won = Extracted && FinalScore >= targetValue;
        SetState(RoundState.Ended);
    }

    // 重新加载当前场景，上一局的状态不会残留。
    private static void Restart()
    {
        Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        // Editor 里的测试场景不一定在 Build Settings 里，用 Editor 专用的方式重新加载。
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.buildIndex);
#endif
    }

    private void FindPlayer()
    {
        if (player != null) return;

        player = FindFirstObjectByType<PlayerState>();
        if (player != null) inventory = player.GetComponent<PlayerInventory>();
    }

    private void SetState(RoundState state)
    {
        State = state;
        StateChanged?.Invoke();
    }

    private static bool Pressed(Keyboard kb, Key key)
    {
        return kb != null && key != Key.None && kb[key].wasPressedThisFrame;
    }
}
