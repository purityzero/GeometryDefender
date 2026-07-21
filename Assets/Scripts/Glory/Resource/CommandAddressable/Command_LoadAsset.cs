using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Command_LoadAsset<T> : ICommand where T : Object
{
    private string m_AssetKey;
    private AsyncOperationHandle<T> m_Handle;
    private bool m_IsFinished;
    private System.Action<T> m_OnLoaded;
    private System.Action<float> m_OnProgress;

    public Command_LoadAsset(string _assetKey, System.Action<T> _onLoaded, System.Action<float> _onProgress = null)
    {
        m_AssetKey = _assetKey;
        m_OnLoaded = _onLoaded;
        m_OnProgress = _onProgress;
    }

    public void Execute()
    {
        m_Handle = Addressables.LoadAssetAsync<T>(m_AssetKey);
        m_IsFinished = false;
    }

    public void Update()
    {
        if (m_Handle.IsValid() && !m_Handle.IsDone)
        {
            m_OnProgress?.Invoke(m_Handle.PercentComplete);
        }

        if (m_Handle.IsDone)
        {
            m_IsFinished = true;
            m_OnLoaded?.Invoke(m_Handle.Result);
        }
    }

    public void Cancel()
    {
        if (m_Handle.IsValid() && !m_Handle.IsDone)
        {
            Addressables.Release(m_Handle);
            m_IsFinished = true;
            Logger.Log($"Load for {m_AssetKey} has been cancelled.");
        }
    }

    public bool IsFinished() => m_IsFinished;
}
