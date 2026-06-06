using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleHexagonEffect : MonoBehaviour
{
    public Image HexagonImage;
    public float Speed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HexagonImage.DOFade(0f, Speed).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
