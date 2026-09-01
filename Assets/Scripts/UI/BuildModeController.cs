using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빌드/상점 모드와 좌클릭 확정을 한곳에서 처리한다.
/// </summary>
public class BuildModeController : MonoBehaviour, IBuildModeState
{
    [SerializeField]
    private TowerSpawner towerSpawner;

    [SerializeField]
    private MapDirector mapDirector;

    [SerializeField]
    private GameObject randomTowerSpawnerImage;

    [Header("Mode Buttons (비우면 이름으로 1회 폴백 검색)")]
    [SerializeField]
    private Button buttonSpawnTower;
    [SerializeField]
    private Button buttonCombineTower;
    [SerializeField]
    private Button buttonSellTower;
    [SerializeField]
    private Button buttonPlaceWall;

    private IPlayerService playerService;
    private BuildMode mode = BuildMode.None;

    private struct ButtonState
    {
        public Button button;
        public Image image;
        public TextMeshProUGUI label;
        public string labelBody;
        public Color normalColor;
        public Sprite normalSprite;
        public Selectable.Transition normalTransition;
        public BuildMode associatedMode;
    }

    private readonly List<ButtonState> modeButtons = new List<ButtonState>();
    private readonly List<(Button button, UnityEngine.Events.UnityAction action)> runtimeListeners =
        new List<(Button, UnityEngine.Events.UnityAction)>();

    public BuildMode CurrentMode => mode;
    public bool HasActiveMode => mode != BuildMode.None;

    protected virtual void Awake()
    {
        ServiceLocator.Register<IBuildModeState>(this);

        if (mapDirector == null)
        {
            mapDirector = MapDirector.Instance;
        }
    }

    protected virtual void OnDestroy()
    {
        for (int i = 0; i < runtimeListeners.Count; i++)
        {
            (Button button, UnityEngine.Events.UnityAction action) = runtimeListeners[i];
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        runtimeListeners.Clear();
    }

    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        if (randomTowerSpawnerImage != null)
        {
            randomTowerSpawnerImage.SetActive(false);
        }

        BindModeButtons();
        RefreshAllButtonVisuals();
    }

    private void BindModeButtons()
    {
        TryBindButton(ref buttonSpawnTower, "ButtonSpawnTower", BuildMode.SpawnTower, bindClick: false);
        TryBindButton(ref buttonCombineTower, "ButtonCombineTower", BuildMode.Combine, bindClick: false);
        TryBindButton(ref buttonSellTower, "ButtonSellTower", BuildMode.Sell, bindClick: false);
        // 벽 버튼은 씬 UnityEvent가 비어 있어서 런타임으로 연결.
        // 타워 3버튼은 씬 UnityEvent(PanelGameManager 메서드)를 그대로 사용.
        TryBindButton(ref buttonPlaceWall, "ButtonPlaceWall", BuildMode.PlaceWall, bindClick: true);
    }

    private void TryBindButton(ref Button button, string fallbackName, BuildMode associatedMode, bool bindClick)
    {
        if (button == null)
        {
            GameObject go = GameObject.Find(fallbackName);
            if (go != null)
            {
                button = go.GetComponent<Button>();
                if (button != null)
                {
                    Debug.LogWarning(
                        $"[BuildModeController] '{fallbackName}'을 이름 검색으로 찾음. 인스펙터에 할당하세요.",
                        this);
                }
            }
        }

        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        modeButtons.Add(new ButtonState
        {
            button = button,
            image = image,
            label = label,
            labelBody = ExtractLabelBody(label != null ? label.text : string.Empty),
            normalColor = image != null ? image.color : Color.white,
            normalSprite = image != null ? image.sprite : null,
            normalTransition = button.transition,
            associatedMode = associatedMode,
        });

        if (bindClick)
        {
            UnityEngine.Events.UnityAction action = PlaceWallButton;
            button.onClick.AddListener(action);
            runtimeListeners.Add((button, action));
        }
    }

    /// <summary>마지막 줄 On/Off를 뺀 본문만 보관.</summary>
    private static string ExtractLabelBody(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string normalized = text.Replace("\r\n", "\n").TrimEnd();
        int lastBreak = normalized.LastIndexOf('\n');
        if (lastBreak < 0)
        {
            return IsOnOffToken(normalized) ? string.Empty : normalized;
        }

        string lastLine = normalized.Substring(lastBreak + 1).Trim();
        if (IsOnOffToken(lastLine))
        {
            return normalized.Substring(0, lastBreak).TrimEnd();
        }

        return normalized;
    }

    private static bool IsOnOffToken(string line)
    {
        return string.Equals(line, "On", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(line, "Off", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyOnOffLabel(TextMeshProUGUI label, string body, bool on)
    {
        if (label == null)
        {
            return;
        }

        string state = on ? "On" : "Off";
        if (string.IsNullOrEmpty(body))
        {
            label.text = state;
            return;
        }

        label.text = body + "\n" + state;
    }

    private void Update()
    {
        if (Camera.main == null)
        {
            return;
        }

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(PointerInput.ScreenPosition());

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelMode();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // 벽 모드: 빈 벽 우클릭 → 철거. 그 외/실패 시 모드 취소. 폰에는 우클릭 없음.
            if (mode == BuildMode.PlaceWall
                && !PointerInput.IsOverUI()
                && mapDirector != null
                && mapDirector.TryRemoveWallAt(mouseWorld))
            {
                // 철거 성공 — 벽 모드 유지
            }
            else
            {
                CancelMode();
            }
        }

        // Space: 벽 모드 진입 (타워 버튼과 같은 흐름). 이미 벽 모드면 즉시 설치.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (mode == BuildMode.PlaceWall)
            {
                TryPlaceWall(mouseWorld);
            }
            else
            {
                PlaceWallButton();
            }
        }

        if (!PointerInput.WasPrimaryPressThisFrame() || !HasActiveMode)
        {
            return;
        }

        if (PointerInput.IsOverUI())
        {
            return;
        }

        HandleModeClick(mouseWorld);
    }

    private void HandleModeClick(Vector2 mouseWorld)
    {
        int layerMask = ~(1 << LayerMask.NameToLayer("NonRayLayer"));
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, Mathf.Infinity, layerMask);

        switch (mode)
        {
            case BuildMode.SpawnTower:
                TrySpawnTower(mouseWorld, hit);
                break;
            case BuildMode.Combine:
                if (hit.transform != null && hit.transform.CompareTag("Tower"))
                {
                    towerSpawner.CombineTower(hit.transform.gameObject);
                }

                break;
            case BuildMode.Sell:
                if (hit.transform != null && hit.transform.CompareTag("Tower"))
                {
                    towerSpawner.CellTower(hit.transform.gameObject);
                }

                break;
            case BuildMode.PlaceWall:
                TryPlaceWall(mouseWorld);
                break;
        }
    }

    private void TrySpawnTower(Vector2 mouseWorld, RaycastHit2D hit)
    {
        if (playerService == null)
        {
            playerService = ServiceLocator.Get<IPlayerService>();
        }

        if (!playerService.TrySpendGold(Constants.spawnRandomTowerGold))
        {
            return;
        }

        if (hit.transform == null || !hit.transform.CompareTag("WallMap"))
        {
            playerService.AddGold(Constants.spawnRandomTowerGold);
            return;
        }

        towerSpawner.SpawnTower(mouseWorld, (TowerGrade)Constants.ShopSpawnGrade);
    }

    private void TryPlaceWall(Vector2 mouseWorld)
    {
        if (!EnsureMapDirector() || mapDirector.IsWallPlacementLocked)
        {
            if (mode == BuildMode.PlaceWall && mapDirector != null && mapDirector.IsWallPlacementLocked)
            {
                CancelMode();
            }

            return;
        }

        mapDirector.TryPlaceWallAt(mouseWorld);
    }

    private bool EnsureMapDirector()
    {
        if (mapDirector == null)
        {
            mapDirector = MapDirector.Instance;
        }

        return mapDirector != null;
    }

    private bool CanEnterWallMode()
    {
        return EnsureMapDirector() && !mapDirector.IsWallPlacementLocked;
    }

    public void CancelMode()
    {
        mode = BuildMode.None;
        if (randomTowerSpawnerImage != null)
        {
            randomTowerSpawnerImage.SetActive(false);
        }

        RefreshAllButtonVisuals();
    }

    private void EnterMode(BuildMode next)
    {
        CancelMode();
        mode = next;
        RefreshAllButtonVisuals();
    }

    private void RefreshAllButtonVisuals()
    {
        for (int i = 0; i < modeButtons.Count; i++)
        {
            ButtonState bs = modeButtons[i];
            if (bs.button == null)
            {
                continue;
            }

            bool pressed = mode == bs.associatedMode;
            ApplyOnOffLabel(bs.label, bs.labelBody, pressed);

            if (bs.image == null)
            {
                continue;
            }

            if (pressed)
            {
                // ColorTint / SpriteSwap 둘 다 덮어쓰지 않게 transition 끔
                bs.button.transition = Selectable.Transition.None;

                Sprite pressedSprite = bs.button.spriteState.pressedSprite;
                if (pressedSprite != null)
                {
                    // SpriteSwap 버튼: Pressed 스프라이트 고정
                    bs.image.sprite = pressedSprite;
                    bs.image.color = bs.normalColor;
                }
                else
                {
                    // ColorTint 버튼: Pressed 색 고정
                    Color pressedColor = bs.button.colors.pressedColor;
                    pressedColor.a = bs.normalColor.a;
                    bs.image.color = pressedColor;
                }
            }
            else
            {
                bs.button.transition = bs.normalTransition;
                bs.image.sprite = bs.normalSprite;
                bs.image.color = bs.normalColor;
            }
        }
    }

    private void ToggleMode(BuildMode target)
    {
        if (mode == target)
        {
            CancelMode();
            return;
        }

        EnterMode(target);
    }

    /// <summary>씬 버튼 / Space — 벽 설치 모드 (좌클릭으로 설치, 연속 가능).</summary>
    public void PlaceWallButton()
    {
        if (!CanEnterWallMode())
        {
            if (mode == BuildMode.PlaceWall)
            {
                CancelMode();
            }

            return;
        }

        ToggleMode(BuildMode.PlaceWall);
    }

    public void RandomTowerSpawnerButton()
    {
        ToggleMode(BuildMode.SpawnTower);
    }

    public void TowerCombinationButton()
    {
        ToggleMode(BuildMode.Combine);
    }

    public void TowerCellButton()
    {
        ToggleMode(BuildMode.Sell);
    }
}
