using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Assets/Design/04_card.html "드래프트 흐름" — [[XpManager]]의 레벨업 신호로 열리고, 3장 중 1장을 고르면 [[CardManager]]가 효과를 적용한다.
public class UICardDraft : UIPopup
{
    [SerializeField] private RectTransform m_CardContainer;
    [SerializeField] private GameObject m_CardTemplate;
    [SerializeField] private Button m_RerollButton;
    [SerializeField] private TextMeshProUGUI m_RerollText;
    [SerializeField] private TextMeshProUGUI m_BuildInfoText;

    private List<GameObject> m_CardInstances = new List<GameObject>();
    private List<CardRecord> m_CurrentDraft = new List<CardRecord>();

    private void Awake()
    {
        m_RerollButton.onClick.AddListener(OnClickReroll);
    }

    public override void Show()
    {
        base.Show();

        Logger.Log("[UICardDraft] Open");

        InGameScene.Current?.SetPaused(true);

        RollAndDisplay();
    }

    public override void Close()
    {
        // InGameScene이 게임오버(m_isGameOver) 상태면 SetPaused(false)를 호출해도 정지 상태가 그대로 유지된다 —
        // 이전엔 Time.timeScale을 직접 1로 되돌려서, 레벨업으로 이 팝업이 열려있다가 카드 선택으로 닫힐 때
        // 게임오버 화면(UIRunOver)이 떠 있는데도 게임이 다시 풀리던 버그가 있었음(2026-07-24, [[InGameScene]] 참고).
        InGameScene.Current?.SetPaused(false);

        base.Close();
    }

    private void RollAndDisplay()
    {
        m_CurrentDraft = InGameScene.Current.cardManager.RollCards();

        DisplayCards();
        RefreshButtons();
        RefreshBuildInfo();

        if (PlayerManager.instance.isAutoMode == true)
        {
            AutoMode();
            return;
        }
    }

    // QA/밸런스 테스트 전용 — 사람이 직접 클릭하지 않고 우선순위(무기 > 공격 > 그 외 일반카드)로 즉시 1장 선택.
    public void AutoMode()
    {
        CardRecord picked = PickCardByPriority();
        if (picked == null)
        {
            Close();
            return;
        }

        InGameScene.Current.cardManager.ApplyCard(picked);
        AdvanceOrClose();
    }

    // 1순위: Weapon 카테고리(무기 해금) / 2순위: Offense 카테고리(공격, 체력·방어 계열 제외) / 3순위: 그 외 남은 카드 중 아무거나.
    private CardRecord PickCardByPriority()
    {
        if (m_CurrentDraft == null || m_CurrentDraft.Count <= 0)
            return null;

        CardRecord weaponCard = m_CurrentDraft.Find(record => record.Category == eCardCategory.Weapon);
        if (weaponCard != null)
            return weaponCard;

        CardRecord offenseCard = m_CurrentDraft.Find(record => record.Category == eCardCategory.Offense);
        if (offenseCard != null)
            return offenseCard;

        return m_CurrentDraft[0];
    }

    private void DisplayCards()
    {
        for (int i = 0; i < m_CardInstances.Count; ++i)
        {
            Destroy(m_CardInstances[i]);
        }
        m_CardInstances.Clear();

        // ResUtil.Create는 소스 오브젝트의 활성 상태를 그대로 복제하므로, 복제 동안만 템플릿을 활성화(UIMetaTree와 동일 관례)
        m_CardTemplate.SetActive(true);

        for (int i = 0; i < m_CurrentDraft.Count; ++i)
        {
            CardRecord record = m_CurrentDraft[i];
            GameObject cardObject = ResUtil.Create(m_CardTemplate, m_CardContainer);
            SetupCard(cardObject, record);
            m_CardInstances.Add(cardObject);
        }

        m_CardTemplate.SetActive(false);
    }

    private void SetupCard(GameObject _cardObject, CardRecord _record)
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        TextMeshProUGUI nameText = _cardObject.transform.Find("Text_Name").GetComponent<TextMeshProUGUI>();
        nameText.SetText(stringTable.GetString(_record.NameKey));

        TextMeshProUGUI effectText = _cardObject.transform.Find("Text_Effect").GetComponent<TextMeshProUGUI>();
        effectText.SetText(stringTable.GetString(_record.EffectKey));

        Image iconImage = _cardObject.transform.Find("Image_Icon").GetComponent<Image>();
        iconImage.color = GetCategoryColor(_record.Category);

        Button cardButton = _cardObject.GetComponent<Button>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => OnClickCard(_record));
    }

    private Color GetCategoryColor(eCardCategory _category)
    {
        switch (_category)
        {
            case eCardCategory.Offense:
                return new Color(0f, 0.8980392f, 1f);
            case eCardCategory.Speed:
                return new Color(1f, 0.85f, 0.2f);
            case eCardCategory.Utility:
                return new Color(1f, 0.2f, 0.8f);
            case eCardCategory.Defense:
                return new Color(0.2f, 0.9f, 0.4f);
            case eCardCategory.Special:
            default:
                return new Color(1f, 0.4f, 0f);
        }
    }

    private void OnClickCard(CardRecord _record)
    {
        InGameScene.Current.cardManager.ApplyCard(_record);

        AdvanceOrClose();
    }

    private void OnClickReroll()
    {
        if (InGameScene.Current.cardManager.CanReroll() == false)
            return;

        InGameScene.Current.cardManager.UseReroll();
        RollAndDisplay();
    }

    // 한 몬스터로 여러 레벨을 동시에 넘긴 경우([[XpManager]].pendingLevelUps) 남은 드래프트가 있으면 새로 굴려서 계속 진행
    private void AdvanceOrClose()
    {
        InGameScene.Current.xpManager?.ConsumePendingLevelUp();

        if (InGameScene.Current.xpManager != null && InGameScene.Current.xpManager.pendingLevelUps > 0)
        {
            RollAndDisplay();
            return;
        }

        Close();
    }

    private void RefreshButtons()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        int remainingRerolls = InGameScene.Current.cardManager.GetRemainingRerolls();
        m_RerollButton.interactable = InGameScene.Current.cardManager.CanReroll();
        m_RerollText.SetText($"{stringTable.GetString("CardDraftRerollButton")} ({remainingRerolls})");
    }

    private void RefreshBuildInfo()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        StringBuilder builder = new StringBuilder();
        eCardCategory[] categories = { eCardCategory.Offense, eCardCategory.Speed, eCardCategory.Utility, eCardCategory.Defense, eCardCategory.Special };

        for (int i = 0; i < categories.Length; ++i)
        {
            eCardCategory category = categories[i];
            int count = InGameScene.Current.cardManager.categoryCounts.TryGetValue(category, out int categoryCount) ? categoryCount : 0;
            if (count <= 0)
                continue;

            if (builder.Length > 0)
                builder.Append(" / ");

            builder.Append($"{stringTable.GetString(GetCategoryLabelKey(category))} {count}");
        }

        m_BuildInfoText.SetText(builder.Length > 0 ? builder.ToString() : stringTable.GetString("CardDraftNoCards"));
    }

    private string GetCategoryLabelKey(eCardCategory _category)
    {
        switch (_category)
        {
            case eCardCategory.Offense:
                return "CardCategoryOffense";
            case eCardCategory.Speed:
                return "CardCategorySpeed";
            case eCardCategory.Utility:
                return "CardCategoryUtility";
            case eCardCategory.Defense:
                return "CardCategoryDefense";
            case eCardCategory.Special:
            default:
                return "CardCategorySpecial";
        }
    }

    public override void OnPressBackBtn()
    {
    }
}
