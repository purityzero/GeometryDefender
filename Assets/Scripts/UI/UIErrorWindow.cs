using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 콘솔을 볼 수 없는 빌드/화면이 깨진 상황에서도 에러 발생 지점을 확인하기 위한 디버그 팝업.
// ErrorLogManager가 Application.logMessageReceived로 에러를 잡아 AddErrorEntry를 호출한다.
public class UIErrorWindow : UIPopup
{
    [SerializeField] private Button m_CloseButton;
    [SerializeField] private ScrollRect m_ScrollRect;
    [SerializeField] private Transform m_EntryContainer;
    [SerializeField] private TextMeshProUGUI m_EntryTemplate;

    public override void Show()
    {
        base.Show();

        m_CloseButton.onClick.RemoveAllListeners();
        m_CloseButton.onClick.AddListener(Close);
    }

    public void AddErrorEntry(string _message, string _stackTrace)
    {
        TextMeshProUGUI entryText = ResUtil.Create(m_EntryTemplate, m_EntryContainer);
        entryText.gameObject.SetActive(true);
        entryText.text = $"[{DateTime.Now:HH:mm:ss}] {_message}\n{_stackTrace}";

        Canvas.ForceUpdateCanvases();
        m_ScrollRect.verticalNormalizedPosition = 0f;
    }
}
