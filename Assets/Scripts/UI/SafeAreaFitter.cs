using UnityEngine;

/// <summary>
/// 이 RectTransform을 Screen.safeArea에 맞춘다. 노치/홈 바 밖으로 HUD가 나가지 않게 한다.
/// 에디터·PC는 safeArea가 보통 전체 화면이라 레이아웃이 그대로다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        rect = (RectTransform)transform;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea == lastSafeArea
            && Screen.width == lastWidth
            && Screen.height == lastHeight)
        {
            return;
        }

        Apply();
    }

    private void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        if (rect == null || lastWidth <= 0 || lastHeight <= 0)
        {
            return;
        }

        Rect safe = lastSafeArea;
        rect.anchorMin = new Vector2(safe.xMin / lastWidth, safe.yMin / lastHeight);
        rect.anchorMax = new Vector2(safe.xMax / lastWidth, safe.yMax / lastHeight);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
