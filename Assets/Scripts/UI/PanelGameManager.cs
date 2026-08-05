using System;

/// <summary>
/// 하위 호환용 래퍼. 기존 씬/UnityEvent 연결을 유지한다.
/// 신규 코드에서는 BuildModeController를 사용.
/// </summary>
[Obsolete("Use BuildModeController instead.")]
public class PanelGameManager : BuildModeController
{
}
