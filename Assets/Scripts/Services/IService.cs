/// <summary>
/// 모든 서비스(매니저)가 공통으로 갖는 초기점.
/// GameBootstrapper가 등록 후 순서대로 Initialize를 호출한다.
/// </summary>
public interface IService
{
    void Initialize();
}
