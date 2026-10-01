using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 按 E 互动：每帧找离玩家最近的可互动目标（物品、保险库门），在它上方显示 "E" 提示。
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Key interactKey = Key.E;
    [Tooltip("互动半径，从玩家中心算起")]
    [SerializeField, Min(0.1f)] private float interactRadius = 1.2f;

    [Header("E 提示")]
    [Tooltip("提示相对于目标顶部的偏移")]
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 0.3f);
    [SerializeField] private Color promptBackground = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color promptTextColor = Color.white;

    private PlayerController controller;
    private readonly List<Collider2D> hits = new List<Collider2D>();
    private ContactFilter2D withTriggers;
    private IInteractable target;
    private Collider2D targetCollider;
    private GameObject prompt;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        withTriggers = ContactFilter2D.noFilter;
        withTriggers.useTriggers = true;
        prompt = CreatePrompt();
    }

    private void OnDestroy()
    {
        if (prompt != null) Destroy(prompt);
    }

    private void Update()
    {
        FindNearestTarget();
        UpdatePrompt();

        Keyboard kb = Keyboard.current;
        if (target != null && kb != null && interactKey != Key.None && kb[interactKey].wasPressedThisFrame)
            target.Interact(gameObject);
    }

    private void FindNearestTarget()
    {
        target = null;
        targetCollider = null;
        if (!controller.CanAct) return;

        Vector2 origin = transform.position;
        int count = Physics2D.OverlapCircle(origin, interactRadius, withTriggers, hits);

        float bestEdge = float.MaxValue;
        float bestCenter = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = hits[i];
            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;

            // 先比到碰撞体边缘的距离（对保险库门这类大物体公平），一样近时再比到中心的距离（物品堆在一起时）。
            float edge = Vector2.Distance(origin, col.ClosestPoint(origin));
            float center = Vector2.Distance(origin, col.bounds.center);
            bool closer = edge < bestEdge - 0.01f || (edge <= bestEdge + 0.01f && center < bestCenter);
            if (!closer) continue;

            bestEdge = edge;
            bestCenter = center;
            target = interactable;
            targetCollider = col;
        }
    }

    private void UpdatePrompt()
    {
        if (target == null)
        {
            prompt.SetActive(false);
            return;
        }

        Bounds bounds = targetCollider.bounds;
        prompt.transform.position = new Vector3(bounds.center.x + promptOffset.x, bounds.max.y + promptOffset.y, 0f);
        prompt.SetActive(true);
    }

    // 用代码生成一个世界空间的小标签：深色方块 + 白色 "E"，用的是 Unity 内置字体。
    private GameObject CreatePrompt()
    {
        var root = new GameObject("InteractPrompt", typeof(RectTransform), typeof(Canvas));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(36f, 36f);
        rootRect.localScale = Vector3.one * 0.01f;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        Stretch((RectTransform)background.transform);
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = promptBackground;
        // 圆角键帽样式。
        backgroundImage.sprite = UIBuilder.RoundedSprite;
        backgroundImage.type = Image.Type.Sliced;
        UIBuilder.SetRadius(backgroundImage, 8f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(root.transform, false);
        Stretch((RectTransform)label.transform);
        var text = label.GetComponent<Text>();
        text.text = "E";
        text.font = UIBuilder.DefaultFont;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = promptTextColor;

        root.SetActive(false);
        return root;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
