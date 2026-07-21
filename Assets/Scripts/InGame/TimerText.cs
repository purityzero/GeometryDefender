using TMPro;
using UnityEngine;

public class TimerText : MonoBehaviour, IUpdatable
{
    [SerializeField] private TextMeshProUGUI m_TimeText;

    private void Start()
    {
        BaseScene.Current.Register(this);
    }

    private void OnDestroy()
    {
        BaseScene.Current?.Unregister(this);
    }

    public void UpdateLogic()
    {
        if (TimerManager.Current == null)
            return;

        int totalSeconds = (int)TimerManager.Current.elapsedTime;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        m_TimeText.text = $"{minutes:00}:{seconds:00}";
    }
}
