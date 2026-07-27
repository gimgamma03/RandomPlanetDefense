/// <summary>
/// 빌드 모드 상태 조회 전용 인터페이스.
/// 입력/액션 제어는 BuildModeController가 담당한다.
/// </summary>
public interface IBuildModeState
{
    BuildMode CurrentMode { get; }
    bool HasActiveMode { get; }
}
