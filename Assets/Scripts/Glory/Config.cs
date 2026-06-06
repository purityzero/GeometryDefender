using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


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

	[MenuItem("ScriptableObject/Config/Create")]
	public static void Create()
	{
		var a = Config.Instance;
	}



}
