using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger
{
	public enum eColor
	{
		Red,
		Green,
		Blue,
		Yellow,
		Orange,
		Purple,
		Gray,
		Cyan,
		White
	}

	public static void Log(string _msg)
	{
		#if UNITY_EDITOR || LOG
		Debug.Log(_msg);
		#endif
	}

	public static void Error(string _msg)
	{
		#if UNITY_EDITOR || LOG
		Debug.LogError(_msg);
		#endif
	}

	public static void Log(string _msg1, string _msg2, eColor eColor)
	{
		Log($"<color={eColor.ToString().ToLower()}>{_msg1}</color> :: <color=white>{_msg2}</color>");
	}

	public static void Error(string _msg1, string _msg2, eColor eColor)
	{
		Error($"<color={eColor.ToString().ToLower()}>{_msg1}</color> :: <color=white>{_msg2}</color>");
	}
}
