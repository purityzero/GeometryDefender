using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Command_DownloadAsset : ICommand
{
    private string m_AssetKey;
    private AsyncOperationHandle m_Handle;
    private bool m_IsFinished;
    private System.Action<float> m_OnProgress;

    public Command_DownloadAsset(string _assetKey, System.Action<float> _onProgress = null)
    {
        m_AssetKey = _assetKey;
        m_OnProgress = _onProgress;
    }

    public void Execute()
    {
        m_Handle = Addressables.DownloadDependenciesAsync(m_AssetKey);
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
        }
    }

    public void Cancel()
    {
        if (m_Handle.IsValid() && !m_Handle.IsDone)
        {
            Addressables.Release(m_Handle);
            m_IsFinished = true;
            Debug.Log($"Download for {m_AssetKey} has been cancelled.");
        }
    }

    public bool IsFinished() => m_IsFinished;
}
