using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Command_CleanupMemory : ICommand
{
    private bool isFinished = false;

    public void Execute()
    {
		#if UNITY_EDITOR || LOG
        Debug.Log("[Command_CleanupMemory] Requesting memory cleanup...");
		#endif
    }

    public void Update()
    {
        Resources.UnloadUnusedAssets(); // 사용되지 않는 리소스 제거
        System.GC.Collect(); // 가비지 컬렉션 실행
		#if UNITY_EDITOR || LOG
        Debug.Log("[Command_CleanupMemory] Memory Cleanup Complete");
		#endif

        isFinished = true;
    }

    public void Cancel() { }

    public bool IsFinished() => isFinished;
}

public class Command_CleanupDontDestroy : ICommand
{
    private bool isFinished = false;

    public void Execute()
    {
        Logger.Log("Command_CleanupDontDestroy", "Cleaning DontDestroyOnLoad Objects...", Logger.eColor.Blue);
    }

    public void Update()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject rootObject in allObjects)
        {
            if (rootObject.transform.parent != null)
                continue;

            if (rootObject.scene.name != "DontDestroyOnLoad")
                continue;

            // 싱글톤 매니저(SceneManager 자신 포함)는 씬을 넘어 유지되어야 하므로 정리 대상에서 제외
            if (HasMonoSingletonInChildren(rootObject) == true)
                continue;

            Object.Destroy(rootObject);
            Logger.Log("Command_CleanupDontDestroy", $"Destroyed {rootObject.name}", Logger.eColor.Blue);
        }

        isFinished = true;
    }

    private bool HasMonoSingletonInChildren(GameObject _rootObject)
    {
        MonoBehaviour[] behaviours = _rootObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            System.Type baseType = behaviour.GetType().BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType == true && baseType.GetGenericTypeDefinition() == typeof(MonoSingleton<>))
                    return true;

                baseType = baseType.BaseType;
            }
        }

        return false;
    }

    public void Cancel() { }

    public bool IsFinished() => isFinished;
}


public class Command_UnloadScene : ICommand
{
    private string sceneName;
    private AsyncOperation unloadOperation;
    private bool isFinished = false;
    private bool isCanceled = false;

    public Command_UnloadScene(string sceneName)
    {
        this.sceneName = sceneName;
    }

    public void Execute()
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Logger.Log("Command_UnloadScene", $"Scene {sceneName} is not loaded.", Logger.eColor.Blue);
            isFinished = true;
            return;
        }

        Logger.Log("Command_UnloadScene", $"Requesting unload: {sceneName}", Logger.eColor.Blue);
        unloadOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
    }

    public void Update()
    {
        if (isFinished || isCanceled || unloadOperation == null)
            return;

        if (unloadOperation.isDone)
        {
            isFinished = true;
            Logger.Log("Command_UnloadScene", $"[Command_UnloadScene] {sceneName} Unload Complete", Logger.eColor.Blue);
        }
    }

    public void Cancel()
    {
        isCanceled = true;
        Logger.Log("Command_UnloadScene",$"{sceneName} Unload Canceled", Logger.eColor.Blue);
    }

    public bool IsFinished() => isFinished;
}

public class Command_LoadScene : ICommand
{
    private string sceneName;
    private AsyncOperation loadOperation;
    private bool isFinished = false;
    private bool isCanceled = false;

    public Command_LoadScene(string sceneName)
    {
        this.sceneName = sceneName;
    }

    public void Execute()
    {
        Logger.Log($"[Command_LoadScene] Requesting load: {sceneName}");
        loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOperation.allowSceneActivation = false;
    }

    public void Update()
    {
        if (isFinished || isCanceled || loadOperation == null)
            return;

        if (loadOperation.progress >= 0.9f)
        {
            loadOperation.allowSceneActivation = true;
            isFinished = true;
            Logger.Log($"[Command_LoadScene] {sceneName} Load Complete");
        }
    }

    public void Cancel()
    {
        isCanceled = true;
        Logger.Log($"[Command_LoadScene] {sceneName} Load Canceled");
    }

    public bool IsFinished() => isFinished;
}

public class SceneManager : MonoSingleton<SceneManager>
{
	public string CurrentScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
	private FlowCommand m_Command = new FlowCommand();

	[SerializeField]
	private Image m_FadeOutObject = null;

    public bool IsSceneTransitioning => !m_Command.IsFinished();
	public void NextScene(string _name)
	{
		string currentScene = CurrentScene;
		Logger.Log("SceneManager", $"Current Scene : {currentScene}, Next Scene :{_name}", Logger.eColor.Green);
		m_FadeOutObject.gameObject.SetActive(true);
		m_FadeOutObject.color = new Color(0, 0, 0, 0);

		m_Command.Add(new Command_Fade(m_FadeOutObject, 1f, 0.2f));
		m_Command.Add(new Command_LoadScene(_name));
		m_Command.Add(new Command_UnloadScene(currentScene));
		m_Command.Add(new Command_CleanupDontDestroy());
		m_Command.Add(new Command_CleanupMemory());
		m_Command.Add(new Command_Fade(m_FadeOutObject, 0f, 0.2f, () => m_FadeOutObject.gameObject.SetActive(false)));
	}

    private void Update()
    {
		m_Command.Update();
	}
}