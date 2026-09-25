using UnityEngine;

// HUD：倒计时、价值、钥匙、容量、速度档位，以及屏幕提示。
// 第 1 步只有外壳：ShowMessage 暂时只打印到 Console（第 5 步补全）。
public class GameHUD : MonoBehaviour
{
    public static void ShowMessage(string text)
    {
        Debug.Log($"[HUD] {text}");
    }
}
