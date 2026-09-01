using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스/터치 공통 포인터. 터치에서 인자 없는 IsPointerOverGameObject()가 실패하는 것을 피한다.
/// </summary>
public static class PointerInput
{
    private static readonly List<RaycastResult> Hits = new List<RaycastResult>(8);
    private static PointerEventData cachedEventData;
    private static EventSystem cachedEventSystem;

    public static bool WasPrimaryPressThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    public static bool IsOverUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (Input.touchCount > 0)
        {
            return GraphicRaycastHits(eventSystem, Input.GetTouch(0).position);
        }

        return eventSystem.IsPointerOverGameObject();
    }

    public static Vector2 ScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }

    private static bool GraphicRaycastHits(EventSystem eventSystem, Vector2 screenPosition)
    {
        if (cachedEventData == null || cachedEventSystem != eventSystem)
        {
            cachedEventSystem = eventSystem;
            cachedEventData = new PointerEventData(eventSystem);
        }

        cachedEventData.Reset();
        cachedEventData.position = screenPosition;
        Hits.Clear();
        eventSystem.RaycastAll(cachedEventData, Hits);

        for (int i = 0; i < Hits.Count; i++)
        {
            if (Hits[i].module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }
}
