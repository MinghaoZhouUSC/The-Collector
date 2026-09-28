using UnityEngine;
using UnityEngine.InputSystem;

// 俯视角移动。速度由 PlayerEncumbrance 决定；被定住或不在游戏中时不能动。
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(PlayerEncumbrance), typeof(PlayerState))]
public class PlayerController : MonoBehaviour
{
    [Header("按键")]
    [SerializeField] private Key upKey = Key.W;
    [SerializeField] private Key downKey = Key.S;
    [SerializeField] private Key leftKey = Key.A;
    [SerializeField] private Key rightKey = Key.D;
    [Tooltip("同时支持方向键")]
    [SerializeField] private bool allowArrowKeys = true;

    private Rigidbody2D body;
    private PlayerEncumbrance encumbrance;
    private PlayerState state;
    private Vector2 moveInput;

    // 当前能否行动：没被定住、背包没打开，并且回合正在进行。测试场景里没有 RoundManager 时视为进行中。
    public bool CanAct
    {
        get
        {
            if (state.IsFrozen || BackpackUI.IsOpen) return false;
            RoundManager round = RoundManager.Instance;
            return round == null || round.State == RoundManager.RoundState.Playing;
        }
    }

    // 在 Editor 里添加组件时自动设置好 Rigidbody2D。
    private void Reset()
    {
        ConfigureBody(GetComponent<Rigidbody2D>());
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        encumbrance = GetComponent<PlayerEncumbrance>();
        state = GetComponent<PlayerState>();
        ConfigureBody(body);

        // 零摩擦：斜着贴墙走时能顺畅滑动，不会被墙"粘住"。
        var circle = GetComponent<CircleCollider2D>();
        if (circle.sharedMaterial == null)
            circle.sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
    }

    // 按键在 Update 里读，速度在 FixedUpdate 里设置。
    private void Update()
    {
        moveInput = ReadMoveInput();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = CanAct ? moveInput * encumbrance.CurrentSpeed : Vector2.zero;
    }

    private Vector2 ReadMoveInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (Held(kb, rightKey) || (allowArrowKeys && kb.rightArrowKey.isPressed)) x += 1f;
        if (Held(kb, leftKey) || (allowArrowKeys && kb.leftArrowKey.isPressed)) x -= 1f;
        if (Held(kb, upKey) || (allowArrowKeys && kb.upArrowKey.isPressed)) y += 1f;
        if (Held(kb, downKey) || (allowArrowKeys && kb.downArrowKey.isPressed)) y -= 1f;

        // 斜向归一化，否则斜着走会快约 41%。
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private static bool Held(Keyboard kb, Key key)
    {
        return key != Key.None && kb[key].isPressed;
    }

    private static void ConfigureBody(Rigidbody2D rb)
    {
        if (rb == null) return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
}
