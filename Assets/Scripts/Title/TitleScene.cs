using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] m_buttonGroup;

    private void Start()
    {
        TableManager.instance.init();

        foreach (var buttonGroup in m_buttonGroup)
        {
            buttonGroup.alpha = 0f;
            buttonGroup.DOFade(1f, 0.5f).SetDelay(0.6f);
        }
    }

    public void OnClickPlayButton()
    {
        SceneManager.instance.NextScene(EScene.InGameScene.ToString());
    }
}
