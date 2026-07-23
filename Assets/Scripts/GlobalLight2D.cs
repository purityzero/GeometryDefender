using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlobalLight2D : MonoBehaviour
{
    [SerializeField] private Light2D m_Light2D;

    private static GlobalLight2D m_instance;

    private void Awake()
    {
        if (m_Light2D == null)
            m_Light2D = GetComponent<Light2D>();

        if (m_instance != null && m_instance != this)
        {
            // OnEnable 경고를 막기 위해 Light2D를 먼저 끄고 파괴
            if (m_Light2D != null)
                m_Light2D.enabled = false;

            Destroy(gameObject);
            return;
        }

        m_instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
