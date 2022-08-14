using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLKitWrapper : MonoBehaviour
{
	public class Result
	{
	}

	public bool Busy { get; private set; }
	public bool Enabled
	{ 
		get
		{
#if UNITY_ANDROID
			return true;
#else
			return false;
#endif			
		}
	}

	public Result GetResult()
	{
		return result;
	}

	public bool RecognizeText(int width, int height, IReadOnlyList<Color32> pixels)
	{
		Debug.Assert(!Busy);
		var input = new Input();
		input.width = width;
		input.height = height;
		input.pixels = new int[pixels.Count];
		for (var i = 0; i < pixels.Count; i++)
		{
			input.pixels[i] = (pixels[i].r << 16) | (pixels[i].g << 8) | pixels[i].b;
		}

		var ret = false;
		EnsureExtractClass();
		try
		{
			var json = JsonUtility.ToJson(input);
			javaClass.CallStatic("RecognizeText", gameObject.name, json);
			ret = true;
			Busy = true;
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		return ret;
	}


	public int TestIncrement(int a)
	{
		EnsureExtractClass();
		var ret = int.MinValue;
		try
		{
			ret = javaClass.CallStatic<int>("IncrementTest", a);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		return ret;
	}

	// non public -----
	[System.Serializable]
	public class Input
	{
		public int width;
		public int height;
		public int[] pixels;
	}
	
	[System.Serializable]
	public class Output
	{
		public string message;
	}

	AndroidJavaClass javaClass;
	Result result;

	void EnsureExtractClass()
	{
		try
		{
			if (javaClass == null)
			{
				javaClass = new AndroidJavaClass("com.hirasho.MLKitWrapper.MLKitWrapper");
			}
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}

	void OnComplete(string outputJson)
	{
		Debug.Log("MLKitWrapper.OnComplete ");

		try
		{
			var output = JsonUtility.FromJson<Output>(outputJson);
			// TODO: Resultの抜き取り
			result = new Result();
		}
		catch (System.Exception e)
		{			
			Debug.LogException(e);
		}
		Busy = false;
	}
}
