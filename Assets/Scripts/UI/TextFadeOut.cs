using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextFadeOut : MonoBehaviour
{
    public float fadeDuration = 1.0f;

    [SerializeField]
    private TextMeshProUGUI showText;

    public TextMeshProUGUI ShowTextTarget => showText;

    /// <summary>
    /// 페이드 인 → 유지 → 페이드 아웃. 웨이브 클리어/보스 등장 배너용.
    /// </summary>
    public void ShowTextFadeInOut(string text, float fadeIn, float hold, float fadeOut)
    {
        if (showText == null)
        {
            Debug.LogWarning("[TextFadeOut] showText 미할당.", this);
            return;
        }

        EnsureActiveInHierarchy();

        showText.text = text;
        Color c = showText.color;
        showText.color = new Color(c.r, c.g, c.b, 0f);

        StopAllCoroutines();
        StartCoroutine(FadeInOutCoroutine(
            Mathf.Max(0f, fadeIn),
            Mathf.Max(0f, hold),
            Mathf.Max(0.01f, fadeOut)));
    }

    public void ShowText(string text, float duration)
    {
        if (showText == null)
        {
            Debug.LogWarning("[TextFadeOut] showText 미할당.", this);
            return;
        }

        EnsureActiveInHierarchy();

        showText.text = text;
        fadeDuration = Mathf.Max(0.01f, duration);

        Color c = showText.color;
        showText.color = new Color(c.r, c.g, c.b, 1f);

        StopAllCoroutines();
        StartCoroutine(FadeOutCoroutine());
    }

    /// <summary>페이드 없이 문구만 고정 표시 (종료 화면용).</summary>
    public void ShowPersistent(string text)
    {
        if (showText == null)
        {
            Debug.LogWarning("[TextFadeOut] showText 미할당.", this);
            return;
        }

        StopAllCoroutines();
        EnsureActiveInHierarchy();

        showText.text = text;
        Color c = showText.color;
        showText.color = new Color(c.r, c.g, c.b, 1f);
    }

    /// <summary>부모가 꺼져 있으면 activeSelf만 켜도 코루틴이 실패한다.</summary>
    private void EnsureActiveInHierarchy()
    {
        Transform t = transform;
        List<Transform> chain = new List<Transform>(8);
        while (t != null)
        {
            chain.Add(t);
            t = t.parent;
        }

        for (int i = chain.Count - 1; i >= 0; i--)
        {
            if (!chain[i].gameObject.activeSelf)
            {
                chain[i].gameObject.SetActive(true);
            }
        }

        EndRunOverlay overlay = GetComponent<EndRunOverlay>();
        if (overlay == null)
        {
            overlay = GetComponentInParent<EndRunOverlay>();
        }

        if (overlay != null)
        {
            overlay.PrepareTransientBanner();
        }
    }

    private IEnumerator FadeInOutCoroutine(float fadeIn, float hold, float fadeOut)
    {
        Color baseColor = showText.color;
        baseColor.a = 1f;

        float elapsed = 0f;
        if (fadeIn > 0f)
        {
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / fadeIn);
                showText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
        }

        showText.color = baseColor;

        if (hold > 0f)
        {
            yield return new WaitForSeconds(hold);
        }

        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeOut);
            showText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            yield return null;
        }

        showText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }

    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0f;
        Color originalColor = showText.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            showText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        showText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }
}
