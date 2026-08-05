using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// PlaySessionStats → ASP.NET POST.
/// 실패해도 플레이에 영향 없음 (로그만).
/// </summary>
public sealed class PlaySessionApiClient : MonoBehaviour
{
    public static PlaySessionApiClient Instance { get; private set; }

    [Header("RpdSessionApi")]
    [SerializeField]
    private string apiBaseUrl = "http://localhost:5026";

    [SerializeField]
    private bool enableUpload = true;

    [SerializeField]
    private string sessionsPath = "/api/sessions";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>세션 종료 직후 호출. 코루틴으로 비동기 전송.</summary>
    public void PostSession(PlaySessionStats stats)
    {
        if (!enableUpload || stats == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            Debug.LogWarning("[PlaySessionApi] apiBaseUrl empty — skip upload.");
            return;
        }

        StartCoroutine(PostSessionRoutine(stats));
    }

    private IEnumerator PostSessionRoutine(PlaySessionStats stats)
    {
        string url = apiBaseUrl.TrimEnd('/') + sessionsPath;
        string json;
        try
        {
            json = JsonConvert.SerializeObject(stats);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlaySessionApi] Serialize failed (ignored): {e.Message}");
            yield break;
        }

        byte[] body = Encoding.UTF8.GetBytes(json);
        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(
                    $"[PlaySessionApi] POST ok session={stats.sessionId} " +
                    $"code={req.responseCode} body={req.downloadHandler?.text}");
            }
            else
            {
                Debug.LogWarning(
                    $"[PlaySessionApi] POST failed (ignored) session={stats.sessionId} " +
                    $"error={req.error} code={req.responseCode}");
            }
        }
    }
}
