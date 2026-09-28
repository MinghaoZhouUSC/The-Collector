using UnityEngine;

// 摄像机平滑跟随目标，挂在 Main Camera 上。没指定目标时自动找 Tag 为 Player 的物体。
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("越小跟得越紧")]
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;

    private Vector3 velocity;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }

        // 开局直接对准目标，避免镜头从别处滑过来。
        if (target != null)
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
    }

    // 在 LateUpdate 里跟随：玩家这一帧移动完之后再移动镜头，画面不抖。
    private void LateUpdate()
    {
        if (target == null) return;

        var goal = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
