using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
	public int minProblemCount = 10;
	public int maxProblemCount = 50;
	public int timeMinute = 3;
	public bool allowZero = true;
	public bool showCubes = true;

	public static SaveData Load()
	{
		var path = SaveDataPathUtil.MakeFullPath(filename);
		var ret = new SaveData();
		if (System.IO.File.Exists(path))
		{
			try
			{
				var json = System.IO.File.ReadAllText(path);
				Debug.Log("[SaveData load]\n" + json);
				JsonUtility.FromJsonOverwrite(json, ret);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
		return ret;
	}

	public void Save()
	{
		var path = SaveDataPathUtil.MakeFullPath(filename);
		try
		{
			var json = JsonUtility.ToJson(this, prettyPrint: true);
			Debug.Log("[SaveData save]\n" + json);
			System.IO.File.WriteAllText(path, json);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}

	// non public --------
	const string filename = "savedata.json";
}
