using TMPro;
using UnityEngine;

public class UIText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private string m_Key;

    private ObservableVariable<eLanguage> m_LanguageObservable;

    private void OnEnable()
    {
        m_LanguageObservable = PlayerManager.instance.languageObservable;
        m_LanguageObservable.RegisterObserver(OnLanguageChanged);
    }

    private void OnDisable()
    {
        m_LanguageObservable.UnregisterObserver(OnLanguageChanged);
    }

    private void OnLanguageChanged(eLanguage _oldLanguage, eLanguage _newLanguage)
    {
        Refresh();
    }

    public void Refresh()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        m_Text.SetText(stringTable.GetString(m_Key).Replace("\\n", "\n"));
    }
}
