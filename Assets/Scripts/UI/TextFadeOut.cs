using System.Collections;
using TMPro;
using UnityEngine;

public class TextFadeOut : MonoBehaviour
{
    public float fadeDuration = 1.0f;

    [SerializeField]
    private TextMeshProUGUI showText;

    public TextMeshProUGUI ShowTextTarget => showText;

    public void ShowText(string text, float duration)
    {
        if (showText == null)
        {
            Debug.LogWarning("[TextFadeOut] showText 미할당.", this);
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

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

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        showText.text = text;
        Color c = showText.color;
        showText.color = new Color(c.r, c.g, c.b, 1f);
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
