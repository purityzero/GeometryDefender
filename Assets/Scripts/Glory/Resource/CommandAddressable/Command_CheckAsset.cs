using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;


public class Command_CheckAsset : ICommand
{
	private string m_key;
	private UnityAction<bool> m_OnComplete = null;
	private AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> m_Handle;
	private bool m_IsFinished;

	public Command_CheckAsset(string _key, UnityAction<bool> _onComplete)
	{
		m_key = _key;
		m_OnComplete = _onComplete;
	}

	public void Execute()
	{
        m_Handle = Addressables.LoadResourceLocationsAsync(m_key);

        m_Handle.Completed += (operation) =>
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                bool exists = operation.Result != null && operation.Result.Count > 0;
                m_OnComplete?.Invoke(exists);
            }
            else
            {
                m_OnComplete?.Invoke(false);
            }

            Addressables.Release(m_Handle);
			m_IsFinished = true;
        };
	}

	public void Update()
	{
	}

	public void Cancel()
	{
        if (m_Handle.IsValid() && !m_Handle.IsDone)
        {
            Addressables.Release(m_Handle);
            m_IsFinished = true;
            Debug.Log($"CheckAsset for {m_key} has been cancelled.");
        }
	}

	public bool IsFinished() => m_IsFinished;

}