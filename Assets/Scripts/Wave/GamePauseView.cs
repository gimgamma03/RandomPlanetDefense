using UnityEngine;
using UnityEngine.UI;

/// <summary>일시정지/재개 버튼 스프라이트 + timeScale.</summary>
public sealed class GamePauseView
{
    private readonly Image startGameButton;
    private readonly Image stopGameButton;
    private readonly Sprite startGameBlackButton;
    private readonly Sprite startGameWhiteButton;
    private readonly Sprite stopGameWhiteButton;
    private readonly Sprite stopGameBlackButton;

    public GamePauseView(
        Image startGameButton,
        Image stopGameButton,
        Sprite startGameBlackButton,
        Sprite startGameWhiteButton,
        Sprite stopGameWhiteButton,
        Sprite stopGameBlackButton)
    {
        this.startGameButton = startGameButton;
        this.stopGameButton = stopGameButton;
        this.startGameBlackButton = startGameBlackButton;
        this.startGameWhiteButton = startGameWhiteButton;
        this.stopGameWhiteButton = stopGameWhiteButton;
        this.stopGameBlackButton = stopGameBlackButton;
    }

    public void Play()
    {
        if (startGameButton != null && startGameBlackButton != null)
        {
            startGameButton.sprite = startGameBlackButton;
        }

        if (stopGameButton != null && stopGameWhiteButton != null)
        {
            stopGameButton.sprite = stopGameWhiteButton;
        }

        Time.timeScale = 1f;
    }

    public void Pause()
    {
        if (startGameButton != null && startGameWhiteButton != null)
        {
            startGameButton.sprite = startGameWhiteButton;
        }

        if (stopGameButton != null && stopGameBlackButton != null)
        {
            stopGameButton.sprite = stopGameBlackButton;
        }

        Time.timeScale = 0f;
    }

    public void ShowIdleStart()
    {
        if (startGameButton != null && startGameWhiteButton != null)
        {
            startGameButton.sprite = startGameWhiteButton;
        }
    }
}
