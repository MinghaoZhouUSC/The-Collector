using UnityEngine;

// 所有能按 E 互动的东西都实现这个接口（物品、保险库门）。
// 实现它的物体上必须有 Collider2D，玩家才能检测到。
public interface IInteractable
{
    // 当前能否互动。例：保险库门打开之后返回 false。
    bool CanInteract { get; }

    // 玩家按 E、且这个物体是离玩家最近的目标时调用。
    void Interact(GameObject player);
}
