using TMPro;
using UnityEngine;

// Time.timeScale을 치트 창에서 바꿔도 실제 기기 성능을 보여줘야 하므로 unscaledDeltaTime으로 계산
public class UIFpsCounter : UpdatableBehaviour
{
    private const float REFRESH_INTERVAL = 0.5f;

    [SerializeField] private TextMeshProUGUI m_FpsText;

    private float m_ElapsedTime;
    private int m_FrameCount;

    public override void UpdateLogic()
    {
        m_ElapsedTime += Time.unscaledDeltaTime;
        m_FrameCount++;

        if (m_ElapsedTime < REFRESH_INTERVAL)
            return;

        float averageFps = m_FrameCount / m_ElapsedTime;
        m_FpsText.text = $"FPS: {averageFps:0}";

        m_ElapsedTime = 0f;
        m_FrameCount = 0;
    }
}
