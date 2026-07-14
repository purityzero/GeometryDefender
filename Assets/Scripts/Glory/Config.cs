using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public enum eSeverType
{
	Local,
}

public class Config : SingletonScriptableObject<Config>
{
	private eSeverType m_ServerType;

	public string Path;
	public eSeverType ServerType
	{
		set
		{
			m_ServerType = value;

			if (value == eSeverType.Local)
				Path = "http://localhost";
		}
	}

#if UNITY_EDITOR
	[MenuItem("ScriptableObject/Config/Create")]
	public static void Create()
	{
		var createdConfig = Config.Instance;
	}
#endif



}
