using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData
{
	public int minProblemCount = 30;
	public int maxProblemCount = 40;
	public int secondsPerProblem = 10;
	public bool allowZero = true;
	public bool showCubes = true;
	public bool useVisionApi = false;

	public static SaveData Load(string encryptionKey)
	{
		var path = SaveDataPathUtil.MakeFullPath(filename);
		var ret = new SaveData();
		if (File.Exists(path))
		{
			byte[] loadBytes = null;
			try
			{
				loadBytes = File.ReadAllBytes(path);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}

			if (loadBytes != null)
			{
				var json = XorEncrypter.Decrypt(loadBytes, encryptionKey);
				Debug.Log("Load(encrypted):\n" + json);					
				try
				{
					JsonUtility.FromJsonOverwrite(json, ret);
				}
				catch (System.Exception e2)
				{
					// たぶん暗号化されてない
					json = System.Text.Encoding.UTF8.GetString(loadBytes); // デコードしないまま
					Debug.Log("Load(raw): " + json);					
					try
					{
						JsonUtility.FromJsonOverwrite(json, ret);
					}
					catch (System.Exception e3)
					{
						Debug.LogException(e2);
						Debug.LogException(e3);
					}
				}
			}
		}
		return ret;
	}

	public void Save(string encryptionKey)
	{
		var path = SaveDataPathUtil.MakeFullPath(filename);
		string json = null;
		try
		{
			json = JsonUtility.ToJson(this, prettyPrint: true);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}

		if (!string.IsNullOrEmpty(json))
		{
			Debug.Log("Save:\n" + json);					
			var bytes = XorEncrypter.Encrypt(json, encryptionKey);
			try
			{
				System.IO.File.WriteAllBytes(path, bytes);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
	}

	// non public --------
	const string filename = "savedata.bin";
}
