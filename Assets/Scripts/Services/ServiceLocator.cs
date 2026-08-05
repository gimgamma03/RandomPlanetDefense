using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service Locator — 매니저를 한곳에서 등록(Register)하고 조회(Get)한다.
/// 사용 예: ServiceLocator.Get&lt;IScoreService&gt;()
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    /// <summary>인터페이스 타입으로 서비스 등록</summary>
    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] 이미 등록됨, 교체합니다: {type.Name}");
        }

        services[type] = service;
    }

    /// <summary>인터페이스로 서비스 조회. 없으면 예외.</summary>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);
        if (services.TryGetValue(type, out object service))
        {
            return service as T;
        }

        throw new InvalidOperationException(
            $"[ServiceLocator] 등록되지 않은 서비스: {type.Name}. GameBootstrapper가 먼저 실행됐는지 확인하세요.");
    }

    /// <summary>등록 여부만 확인 (예외 없음)</summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        if (services.TryGetValue(typeof(T), out object found) && found is T typed)
        {
            service = typed;
            return true;
        }

        service = null;
        return false;
    }

    public static bool IsRegistered<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }

    /// <summary>테스트·씬 완전 리셋용</summary>
    public static void Clear()
    {
        services.Clear();
    }
}
