using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
    private static T m_Instance;

    public static T instance
    {
        get
        {
            // 파괴된 인스턴스(유니티 가짜 null)도 재생성 대상
            if (m_Instance == null)
            {
                m_Instance = FindFirstObjectByType<T>();
                if (m_Instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    m_Instance = singletonObject.AddComponent<T>();
                }
            }

            return m_Instance;
        }
    }

    protected virtual void Awake()
    {
        // AddComponent 시점엔 getter가 m_Instance를 할당하기 전에 Awake가 먼저 불린다 — 여기서 직접 할당
        if (m_Instance == null || m_Instance == this)
        {
            m_Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 중복된 싱글톤 제거
        }
    }

}
