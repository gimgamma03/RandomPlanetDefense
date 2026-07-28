using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>월드 공간 데미지 숫자. IPoolService(PoolId.DamagePopup)로 대여·반환.</summary>
public sealed class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 1.0f;
    private const float RiseDistance = 0.55f;
    private const float PunchDuration = 0.1f;
    private const float PunchScale = 1.35f;

    [SerializeField]
    private TextMeshPro text;

    private Coroutine playRoutine;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TextMeshPro>();
        }

        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.0001f)
        {
            baseScale = Vector3.one;
        }
    }

    public void EnsureText(TMP_FontAsset font)
    {
        if (text == null)
        {
            text = GetComponent<TextMeshPro>();
        }

        if (text == null)
        {
            text = gameObject.AddComponent<TextMeshPro>();
        }

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 3.2f;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.sortingOrder = 220;
        if (font != null)
        {
            text.font = font;
        }
    }

    public void Play(float amount, Color color, Vector3 worldPosition)
    {
        if (text == null)
        {
            text = GetComponent<TextMeshPro>();
        }

        if (text == null)
        {
            DamagePopupSpawner.Release(this);
            return;
        }

        string label = DamagePopupSpawner.FormatAmount(amount);
        if (label == null)
        {
            DamagePopupSpawner.Release(this);
            return;
        }

        transform.position = worldPosition + new Vector3(0f, 0.25f, 0f);
        transform.localScale = baseScale;
        text.text = label;
        text.color = color;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * RiseDistance;
        Color baseColor = text.color;
        float elapsed = 0f;

        while (elapsed < Lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Lifetime);
            transform.position = Vector3.Lerp(start, end, t);

            // 초반 짧게 커졌다가 원래 크기
            if (elapsed < PunchDuration)
            {
                float punchT = elapsed / PunchDuration;
                float scale = Mathf.Lerp(PunchScale, 1f, punchT);
                transform.localScale = baseScale * scale;
            }
            else
            {
                transform.localScale = baseScale;
            }

            float alpha = 1f - t;
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        playRoutine = null;
        transform.localScale = baseScale;
        DamagePopupSpawner.Release(this);
    }

    public void ClearForPool()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        transform.localScale = baseScale;
        if (text != null)
        {
            text.text = string.Empty;
        }
    }
}
