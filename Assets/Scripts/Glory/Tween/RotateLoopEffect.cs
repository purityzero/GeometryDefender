using DG.Tweening;
using UnityEngine;

/// <summary>
/// Y축 한 바퀴 회전 → 쉬고 → 반대로 한 바퀴 → 쉬고를 무한 반복하는 연출.
/// 기존 회전값(z -90 등)을 건드리지 않도록 LocalAxisAdd(상대 회전)를 사용한다.
/// </summary>
public class RotateLoopEffect : MonoBehaviour
{
    [SerializeField] private float m_RotateDuration = 2f;
    [SerializeField] private float m_RestDuration = 0.5f;
    [SerializeField] Ease m_Ease = Ease.Linear;
    [SerializeField] private Vector3 m_RotationValue = Vector3.zero;

    private Sequence m_Sequence;

    private void OnEnable()
    {
        m_Sequence = TweenSequenceBuilder.Create()
            .Append(TweenUtil.RotateLocal(transform, m_RotationValue, m_RotateDuration, RotateMode.LocalAxisAdd).SetEase(m_Ease))
            .Delay(m_RestDuration)
            .Append(TweenUtil.RotateLocal(transform, -m_RotationValue, m_RotateDuration, RotateMode.LocalAxisAdd).SetEase(m_Ease))
            .Delay(m_RestDuration)
            .Loops(-1)
            .Play();
    }

    private void OnDisable()
    {
        if (m_Sequence != null)
        {
            m_Sequence.Kill();
            m_Sequence = null;
        }
    }
}
