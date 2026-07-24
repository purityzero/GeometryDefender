using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 07_ui.html "데미지 텍스트" — 적군/아군 피격 시 월드 스페이스 숫자 팝업. 초당 스폰 수를 throttle해 스팸을 막는다.
public class DamageTextManager : UpdatableBehaviour
{
    private const string PREFAB_PATH = "Prefabs/Effect/DamageText";
    private const string CRIT_EXPLOSION_PREFAB_PATH = "Prefabs/Effect/CritExplosion";
    private const string SPLASH_EXPLOSION_PREFAB_PATH = "Prefabs/Effect/SplashExplosion";
    private const string CHAIN_LIGHTNING_PREFAB_PATH = "Prefabs/Effect/ChainLightning";

    [SerializeField] private Transform m_PoolParent;

    private MemoryPooling<DamageText> m_Pool;
    private MemoryPooling<CritExplosion> m_CritExplosionPool;
    private MemoryPooling<SplashExplosion> m_SplashExplosionPool;
    private MemoryPooling<ChainLightning> m_ChainLightningPool;

    private int m_SpawnCountThisSecond;
    private float m_SecondTimer;

    [System.NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        m_Pool = new MemoryPooling<DamageText>(GameConfigTable.DAMAGE_TEXT_POOL_SIZE, PREFAB_PATH, m_PoolParent);
        m_Pool.Prewarm();

        m_CritExplosionPool = new MemoryPooling<CritExplosion>(GameConfigTable.CRIT_EXPLOSION_POOL_SIZE, CRIT_EXPLOSION_PREFAB_PATH, m_PoolParent);
        m_CritExplosionPool.Prewarm();

        m_SplashExplosionPool = new MemoryPooling<SplashExplosion>(GameConfigTable.SPLASH_EXPLOSION_POOL_SIZE, SPLASH_EXPLOSION_PREFAB_PATH, m_PoolParent);
        m_SplashExplosionPool.Prewarm();

        m_ChainLightningPool = new MemoryPooling<ChainLightning>(GameConfigTable.CHAIN_LIGHTNING_POOL_SIZE, CHAIN_LIGHTNING_PREFAB_PATH, m_PoolParent);
        m_ChainLightningPool.Prewarm();

        m_isInitialized = true;
    }

    public void ShowEnemyDamage(Vector3 _position, int _amount, bool _isCrit)
    {
        if (PlayerManager.instance.optionData.isEnemyDamageTextOn == false)
            return;

        Spawn(_position, _amount, _isCrit, false);
    }

    public void ShowAllyDamage(Vector3 _position, int _amount)
    {
        if (PlayerManager.instance.optionData.isAllyDamageTextOn == false)
            return;

        Spawn(_position, _amount, false, true);
    }

    private void Spawn(Vector3 _position, int _amount, bool _isCrit, bool _isAllyDamage)
    {
        if (m_isInitialized == false)
            return;

        if (m_SpawnCountThisSecond >= GameConfigTable.DAMAGE_TEXT_MAX_SPAWN_PER_SECOND)
            return;

        DamageText damageText = m_Pool.Pop();
        if (damageText == null)
            return;

        ++m_SpawnCountThisSecond;

        damageText.Open();
        damageText.transform.position = _position;
        damageText.Play(_amount, _isCrit, _isAllyDamage, OnDamageTextComplete);
    }

    private void OnDamageTextComplete(DamageText _damageText)
    {
        _damageText.Close();
        m_Pool.Push(_damageText);
    }

    // 02_combat.html "치명타 시스템" — 치명타로 적을 처치했을 때 호출(HealthSystem)
    public void ShowCritExplosion(Vector3 _position)
    {
        if (m_isInitialized == false)
            return;

        CritExplosion critExplosion = m_CritExplosionPool.Pop();
        if (critExplosion == null)
            return;

        critExplosion.Open();
        critExplosion.transform.position = _position;
        critExplosion.Play(OnCritExplosionComplete);
    }

    private void OnCritExplosionComplete(CritExplosion _critExplosion)
    {
        _critExplosion.Close();
        m_CritExplosionPool.Push(_critExplosion);
    }

    // 02_combat.html "투사체 종류" — Splash 카드로 명중했을 때 호출(ProjectileCollisionSystem)
    public void ShowSplashExplosion(Vector3 _position)
    {
        if (m_isInitialized == false)
            return;

        SplashExplosion splashExplosion = m_SplashExplosionPool.Pop();
        if (splashExplosion == null)
            return;

        splashExplosion.Open();
        splashExplosion.transform.position = _position;
        splashExplosion.Play(OnSplashExplosionComplete);
    }

    private void OnSplashExplosionComplete(SplashExplosion _splashExplosion)
    {
        _splashExplosion.Close();
        m_SplashExplosionPool.Push(_splashExplosion);
    }

    // 02_combat.html "투사체 종류" — Chain 카드로 튄 경로를 호출(ProjectileCollisionSystem). _points는 명중 지점부터 순서대로.
    public void ShowChainLightning(List<Vector3> _points)
    {
        if (m_isInitialized == false)
            return;

        ChainLightning chainLightning = m_ChainLightningPool.Pop();
        if (chainLightning == null)
            return;

        chainLightning.Open();
        chainLightning.Play(_points, OnChainLightningComplete);
    }

    private void OnChainLightningComplete(ChainLightning _chainLightning)
    {
        _chainLightning.Close();
        m_ChainLightningPool.Push(_chainLightning);
    }

    // 02_combat.html "치명타 발생 시... 화면 미세 셰이크"
    // 셰이크가 끝나도 부동소수점 오차 등으로 카메라가 원위치(0,0,-10)에 정확히 안 돌아오는 경우가 있어(2026-07-24 사용자 실측 피드백),
    // 완료 콜백에서 강제로 스냅.
    public void ShakeCamera()
    {
        if (Camera.main == null)
            return;

        Transform cameraTransform = Camera.main.transform;

        TweenUtil.ShakePosition(cameraTransform, GameConfigTable.CRIT_SHAKE_DURATION, GameConfigTable.CRIT_SHAKE_STRENGTH, GameConfigTable.CRIT_SHAKE_VIBRATO)
            .OnComplete(() => cameraTransform.position = new Vector3(0f, 0f, -10f));
    }

    // 02_combat.html "치명타 발생 시... 진동 (모바일)" — 한 번보다 두 번 빠르게 울려야 더 잘 느껴진다는 사용자 피드백으로
    // 즉시 1회 + VibratePulseInterval(GameConfigTable) 뒤 1회 더(총 2연타). 지원 안 하는 플랫폼(에디터 등)에서는 Handheld.Vibrate()가 조용히 무시됨.
    public void VibrateOnCrit()
    {
        if (PlayerManager.instance.optionData.isHapticOn == false)
            return;
            
        Handheld.Vibrate();
        TweenUtil.DelayedCall(GameConfigTable.VIBRATE_PULSE_INTERVAL, () => Handheld.Vibrate());
    }

    public override void UpdateLogic()
    {
        m_SecondTimer += Time.deltaTime;
        if (m_SecondTimer < 1f)
            return;

        m_SecondTimer = 0f;
        m_SpawnCountThisSecond = 0;
    }
}
