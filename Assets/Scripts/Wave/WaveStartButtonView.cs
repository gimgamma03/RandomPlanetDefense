using UnityEngine;
using UnityEngine.UI;

/// <summary>웨이브 시작 버튼 busy/idle 표시만 담당.</summary>
public sealed class WaveStartButtonView
{
    private Button button;
    private Color idleColor = Color.white;
    private Color busyColor = new Color(0.35f, 0.85f, 0.95f, 1f);
    private Vector3 busyScale = new Vector3(0.92f, 0.88f, 1f);

    private Sprite idleSprite;
    private Sprite busySprite;
    private Vector3 idleScale = Vector3.one;

    public void Bind(
        Button waveStartButton,
        Transform searchRoot,
        Color idleColor,
        Color busyColor,
        Vector3 busyScale)
    {
        button = waveStartButton;
        this.idleColor = idleColor;
        this.busyColor = busyColor;
        this.busyScale = busyScale;

        if (button == null && searchRoot != null)
        {
            Transform found = searchRoot.Find("WaveStart");
            if (found == null)
            {
                found = FindDeep(searchRoot, "WaveStart");
            }

            if (found != null)
            {
                button = found.GetComponent<Button>();
            }
        }

        if (button == null)
        {
            return;
        }

        idleScale = button.transform.localScale;
        Image image = ResolveImage();
        if (image != null)
        {
            idleSprite = image.sprite;
        }

        Sprite pressed = button.spriteState.pressedSprite;
        busySprite = pressed != null ? pressed : idleSprite;
    }

    public void SetBusy(bool busy)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = !busy;

        Image image = ResolveImage();
        if (image != null)
        {
            if (busy)
            {
                if (busySprite != null)
                {
                    image.sprite = busySprite;
                }

                image.color = busyColor;
            }
            else
            {
                if (idleSprite != null)
                {
                    image.sprite = idleSprite;
                }

                image.color = idleColor;
            }
        }

        button.transform.localScale = busy
            ? Vector3.Scale(idleScale, busyScale)
            : idleScale;
    }

    public void Lock()
    {
        SetBusy(true);
        if (button != null)
        {
            button.interactable = false;
        }
    }

    private Image ResolveImage()
    {
        if (button == null)
        {
            return null;
        }

        Image image = button.targetGraphic as Image;
        return image != null ? image : button.GetComponent<Image>();
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
