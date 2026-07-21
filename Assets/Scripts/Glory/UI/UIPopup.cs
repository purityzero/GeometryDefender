public abstract class UIPopup : UIBase
{
    public override void Show()
    {
        base.Show();
        UIManager.instance.RegisterPopup(this);
    }

    public override void Close()
    {
        UIManager.instance.UnregisterPopup(this);
        base.Close();
    }

    // 기본은 닫기 — 뒤로가기로 닫히면 안 되는 팝업은 이 메서드를 오버라이드해서 원하는 동작으로 대체할 것
    public virtual void OnPressBackBtn()
    {
        Close();
    }
}
