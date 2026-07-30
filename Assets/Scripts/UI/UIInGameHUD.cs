using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInGameHUD : UIBase
{
    [SerializeField] private TextMeshProUGUI m_HpText;
    [SerializeField] private TextMeshProUGUI m_TimeText;
    [SerializeField] private TextMeshProUGUI m_KillText;
    [SerializeField] private TextMeshProUGUI m_WaveText;
    [SerializeField] private TextMeshProUGUI m_LevelText;
    [SerializeField] private Image m_XpFillImage;
    [SerializeField] private Image m_HpFillImage;

    [Header("# 무기 쿨다운")]
    [SerializeField] private Transform m_WeaponCooldownContainer;
    [SerializeField] private GameObject m_WeaponCooldownTemplate;

    private ObservableVariable<int> m_HpObservable;
    private ObservableVariable<int> m_KillObservable;
    private ObservableVariable<int> m_XpObservable;

    private WaveTable m_WaveTable;

    private List<Image> m_WeaponCooldownFills = new List<Image>();

    private void OnDestroy()
    {
        if (m_HpObservable != null)
            m_HpObservable.UnregisterObserver(OnHpChanged);

        if (m_KillObservable != null)
            m_KillObservable.UnregisterObserver(OnKillChanged);

        if (m_XpObservable != null)
            m_XpObservable.UnregisterObserver(OnXpChanged);
    }

    public override void UpdateLogic()
    {
        UpdateTimeText();
        UpdateWaveText();
        UpdateLevelText();
        TryRegisterHpObservable();
        TryRegisterKillObservable();
        TryRegisterXpObservable();
        UpdateWeaponCooldowns();
    }

    private void UpdateTimeText()
    {
        if (InGameScene.Current == null || InGameScene.Current.timerManager == null)
            return;

        int totalSeconds = (int)InGameScene.Current.timerManager.elapsedTime;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        m_TimeText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateWaveText()
    {
        if (InGameScene.Current == null || InGameScene.Current.timerManager == null)
            return;

        if (m_WaveTable == null)
            m_WaveTable = TableManager.instance.GetTable<WaveTable>();

        if (m_WaveTable == null)
            return;

        int elapsedSeconds = (int)InGameScene.Current.timerManager.elapsedTime;
        WaveRecord activePhase = m_WaveTable.GetActivePhase(elapsedSeconds);
        if (activePhase == null)
            return;

        // Infinite 난이도는 마지막 정의 웨이브 이후로도 시간이 계속 흐르는데, WaveTable엔 그 이후 웨이브가 없어
        // GetActivePhase()가 항상 마지막 Id를 반환한다 — 난이도가 계속 오르는 걸 표시에도 반영하도록 스텝 수만큼 더해준다.
        int displayWaveId = activePhase.Id;
        if (InGameScene.Current.difficultyManager != null)
            displayWaveId += (int)InGameScene.Current.difficultyManager.GetInfiniteStepCount();

        m_WaveText.text = $"WAVE {displayWaveId}";
    }

    // Wave와 동일한 이유로 폴링 유지(값이 자주 안 바뀌지만 조회 자체가 가벼움) — 사용자 요청("유저의 레벨 숫자도 표기했으면 좋겠음").
    private void UpdateLevelText()
    {
        if (InGameScene.Current == null || InGameScene.Current.xpManager == null)
            return;

        m_LevelText.text = $"LV.{InGameScene.Current.xpManager.currentLevel}";
    }

    private void TryRegisterHpObservable()
    {
        if (m_HpObservable != null)
            return;

        if (InGameScene.Current == null || InGameScene.Current.towerController == null)
            return;

        m_HpObservable = InGameScene.Current.towerController.currentHp;
        m_HpObservable.RegisterObserver(OnHpChanged);
    }

    private void TryRegisterKillObservable()
    {
        if (m_KillObservable != null)
            return;

        if (InGameScene.Current == null || InGameScene.Current.monsterManager == null)
            return;

        m_KillObservable = InGameScene.Current.monsterManager.killCount;
        m_KillObservable.RegisterObserver(OnKillChanged);
    }

    private void OnHpChanged(int _oldValue, int _newValue)
    {
        if (InGameScene.Current == null || InGameScene.Current.towerController == null)
            return;

        int maxHp = InGameScene.Current.towerController.maxHp;

        m_HpText.text = $"{_newValue}/{maxHp}";

        if (maxHp > 0)
            m_HpFillImage.fillAmount = (float)_newValue / maxHp;
    }

    private void OnKillChanged(int _oldValue, int _newValue)
    {
        m_KillText.text = _newValue.ToString();
    }

    private void TryRegisterXpObservable()
    {
        if (m_XpObservable != null)
            return;

        if (InGameScene.Current == null || InGameScene.Current.xpManager == null)
            return;

        m_XpObservable = InGameScene.Current.xpManager.currentXp;
        m_XpObservable.RegisterObserver(OnXpChanged);
    }

    private void OnXpChanged(int _oldValue, int _newValue)
    {
        if (InGameScene.Current == null || InGameScene.Current.xpManager == null || InGameScene.Current.xpManager.requiredXp <= 0)
            return;

        m_XpFillImage.fillAmount = (float)_newValue / InGameScene.Current.xpManager.requiredXp;
    }

    // 무기는 카드로 계속 늘어나기만 하므로(줄어들 일 없음), 새로 생긴 무기만큼만 행을 추가로 만들고 나머지는 매 프레임 fillAmount만 갱신 —
    // ActorPlayer.GetWeaponCooldownRatio()는 쿨다운마다 0(방금 발사)→1(재장전 완료)로 차오르는 값이라 XP 게이지와 동일한 방식으로 표현.
    private void UpdateWeaponCooldowns()
    {
        if (InGameScene.Current == null || InGameScene.Current.towerController == null)
            return;

        ActorPlayer towerController = InGameScene.Current.towerController;

        while (m_WeaponCooldownFills.Count < towerController.weaponCount)
        {
            int index = m_WeaponCooldownFills.Count;

            GameObject row = ResUtil.Create(m_WeaponCooldownTemplate, m_WeaponCooldownContainer);
            row.SetActive(true);

            StringTable stringTable = TableManager.instance.GetTable<StringTable>();
            string weaponName = (stringTable != null) ? stringTable.GetString(towerController.GetWeaponNameKey(index)) : towerController.GetWeaponNameKey(index);

            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
            label.text = weaponName;

            Image fill = row.transform.Find("Image_GaugeBG/Image_GaugeFill").GetComponent<Image>();
            if (ColorUtility.TryParseHtmlString(towerController.GetWeaponColorHex(index), out Color weaponColor) == true)
            {
                weaponColor.a = towerController.GetWeaponAlpha(index);
                fill.color = weaponColor;

                // 사용자 요청("반짝거리는 거 없는애들은 그냥 color값 적용, 냉기오브 같은 반짝거리는 효과 있는애들은 tween") —
                // 인게임 오브젝트 쪽에 실제로 글로우 펄스가 있는 무기(Frost Orb Turret)만 게이지도 같은 톤으로 Tween, 나머지는 정적 색상 그대로.
                if (towerController.GetWeaponHasGlowPulse(index) == true)
                {
                    Color brighterColor = Color.Lerp(weaponColor, Color.white, 0.6f);
                    TweenUtil.Color(fill, brighterColor, GameConfigTable.ORBITAL_SLOW_GLOW_PULSE_DURATION)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            }

            m_WeaponCooldownFills.Add(fill);
        }

        for (int i = 0; i < m_WeaponCooldownFills.Count; ++i)
        {
            m_WeaponCooldownFills[i].fillAmount = towerController.GetWeaponCooldownRatio(i);
        }
    }

    public void OnClickPauseButton()
    {
        UIManager.instance.Get<UIPause>();
    }

    public void OnClickCheatButton()
    {
        UIManager.instance.Get<UICheatWindow>();
    }
}
