using UnityEngine;

/// <summary>클리어/게임오버 메시지 + Title 버튼 오버레이 표시.</summary>
public sealed class EndRunPresenter
{
    private TextFadeOut textFadeOut;
    private EndRunOverlay endRunOverlay;
    private readonly MonoBehaviour host;

    public EndRunPresenter(MonoBehaviour host, TextFadeOut textFadeOut, EndRunOverlay endRunOverlay)
    {
        this.host = host;
        this.textFadeOut = textFadeOut;
        this.endRunOverlay = endRunOverlay;
    }

    public void EnsureBuilt()
    {
        if (endRunOverlay != null)
        {
            endRunOverlay.EnsureBuilt();
            return;
        }

        if (textFadeOut != null)
        {
            endRunOverlay = textFadeOut.GetComponent<EndRunOverlay>();
            if (endRunOverlay == null)
            {
                endRunOverlay = textFadeOut.gameObject.AddComponent<EndRunOverlay>();
            }
        }
        else if (host != null)
        {
            endRunOverlay = host.GetComponentInChildren<EndRunOverlay>(true);
        }

        endRunOverlay?.EnsureBuilt();
    }

    public void Show(string message)
    {
        EnsureBuilt();

        if (textFadeOut != null)
        {
            textFadeOut.ShowPersistent(message);
        }

        if (endRunOverlay != null)
        {
            endRunOverlay.Show(message);
            return;
        }

        if (textFadeOut == null)
        {
            Debug.LogWarning("[EndRunPresenter] EndRunOverlay / TextFadeOut 없음: " + message);
        }
    }
}
