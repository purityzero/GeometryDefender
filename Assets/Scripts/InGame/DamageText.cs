using UnityEngine;

// 07_ui.html "데미지 텍스트" — 피격 시 작은 숫자, 0.5초 페이드아웃 + 위로 살짝 이동. 치명타는 1.5배 크기 + 노란색.
public class DamageText : FactoryObject
{
    private static readonly Color ENEMY_DAMAGE_COLOR = Color.white;
    private static readonly Color ALLY_DAMAGE_COLOR = new Color(1f, 0.35f, 0.3f);
    private static readonly Color CRIT_COLOR = Color.yellow;

    [SerializeField] private TMPro.TextMeshPro m_Text;

    public void Play(int _amount, bool _isCrit, bool _isAllyDamage, System.Action<DamageText> _onComplete)
    {
        m_Text.text = _amount.ToString();
        m_Text.alpha = 1f;
        m_Text.color = (_isCrit == true) ? CRIT_COLOR : ((_isAllyDamage == true) ? ALLY_DAMAGE_COLOR : ENEMY_DAMAGE_COLOR);

        float scale = (_isCrit == true) ? GameConfigTable.DAMAGE_TEXT_CRIT_SCALE : 1f;
        transform.localScale = Vector3.one * scale;

        Vector3 targetPosition = transform.position + new Vector3(0f, GameConfigTable.DAMAGE_TEXT_MOVE_UP_DISTANCE, 0f);

        TweenSequenceBuilder.Create()
            .Append(TweenUtil.Move(transform, targetPosition, GameConfigTable.DAMAGE_TEXT_FADE_DURATION))
            .Join(TweenUtil.Fade(m_Text, 0f, GameConfigTable.DAMAGE_TEXT_FADE_DURATION))
            .OnComplete(() => _onComplete?.Invoke(this))
            .Play();
    }
}
