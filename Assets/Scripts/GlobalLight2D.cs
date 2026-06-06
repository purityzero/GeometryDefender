using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlobalLight2D : MonoBehaviour
{
    private static GlobalLight2D m_instance;

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            // OnEnable 경고를 막기 위해 Light2D를 먼저 끄고 파괴
            Light2D light2D = GetComponent<Light2D>();
            if (light2D != null)
                light2D.enabled = false;

            Destroy(gameObject);
            return;
        }

        m_instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
