//#define DUMMY_MODE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLKitWrapper : MonoBehaviour
{
	[System.Serializable]
	public class Text
	{
		public string text;
		public TextBlock[] textBlocks;
	}

	[System.Serializable]
	public class TextBlock
	{
		public Rect boundingBox;
		public Point[] cornerPoints;
		public Line[] lines;
		public string recognizedLanguage;
		public string text;
	}

	[System.Serializable]
	public class Line
	{
		public Rect boundingBox;
		public Point[] cornerPoints;
		public Element[] elements;
		public string recognizedLanguage;
		public string text;
	}

	[System.Serializable]
	public class Element
	{
		public Rect boundingBox;
		public Point[] cornerPoints;
		public string recognizedLanguage;
		public string text;
	}

	[System.Serializable]
	public class Rect
	{
		public int bottom;
		public int left;
		public int right;
		public int top;
	}

	[System.Serializable]
	public class Point
	{
		public int x;
		public int y;
	}

	public bool Requested { get; private set; }
	public string ErrorMessage { get; private set; }
	public bool Enabled
	{ 
		get
		{
			var ret = false;
#if UNITY_EDITOR // Editorでは無効
#elif UNITY_ANDROID
			ret = true;
#endif			

#if DUMMY_MODE
			ret = true;
#endif
			return ret;
		}
	}

	public bool IsDone()
	{
Debug.Log("MLKit: IsDone " + Requested + " " + waitingRequestId);
		return Requested && (waitingRequestId == int.MinValue);
	}

	public void Abort()
	{
Debug.Log("MLKit: Abort");
		Requested = false;
		waitingRequestId = int.MinValue;
	}

	public Text GetResult()
	{
		return result;
	}

	public bool RecognizeText(int width, int height, IReadOnlyList<Color32> pixels)
	{
Debug.Log("MLKit Recognize: " + width + "x" + height + " pixelCount=" + pixels.Count);
		if (!IsDone()) // 前のが終わってないので止める
		{
			Abort();
		}
		result = null;

		var input = new Input();
		input.width = width;
		input.height = height;
		input.pixels = new int[pixels.Count];
		input.requestId = nextRequestId;
		waitingRequestId = nextRequestId;

		// Y反転しながら送る
		for (var y = 0; y < height; y++)
		{
			var dstY = height - y - 1;
			for (var x = 0; x < width; x++)
			{
				var src = pixels[(y * width) + x];
				input.pixels[(dstY * width) + x] = (0xff << 24) | (src.r << 16) | (src.g << 8) | src.b;
			}
		}

		var ret = false;
		EnsureExtractClass();
		try
		{
			// jsonにすると遅いので、stringにバイナリデータ詰める

			string serializedInput = null;
#if true
			serializedInput = input.Serialize();
#else
			var json = JsonUtility.ToJson(input, true);
Debug.Log("MLKit input jsonSize=" + json.Length + " req=" + Requested);
#if UNITY_EDITOR
System.IO.File.WriteAllText("mlkitInput.json", json);
#endif
#endif
			ret = true;
			Requested = true;
			nextRequestId++;
			if (nextRequestId == int.MinValue) // 来ないと思うが巻き戻った時のための対処
			{
				nextRequestId++;
			}

#if DUMMY_MODE
			StartCoroutine(CoOnCompleteDummy());
#else
			javaClass.CallStatic("recognizeText", gameObject.name, serializedInput);
#endif
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
			ret = javaClass.CallStatic<int>("incrementTest", a);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		return ret;
	}

	// non public -----
	[System.Serializable]
	class Input
	{
		// Jsonだと遅いのでバイナリでString内にSerializeする
		public string Serialize()
		{
			var sb = new System.Text.StringBuilder();
			Append(sb, requestId);
			Append(sb, width);
			Append(sb, height);
			var n = width * height;
			Debug.Assert(n == pixels.Length);
			for (var i = 0; i < n; i++)
			{
				Append(sb, pixels[i]);
			}
			return sb.ToString();
		}
		public int requestId;
		public int width;
		public int height;
		public int[] pixels;

		// non public ----
		void Append(System.Text.StringBuilder sb, int v)
		{
			sb.Append((char)(v & 0xffff)); //Low
			v >>= 16;
			sb.Append((char)(v & 0xffff)); //High
		}
	}
	
	[System.Serializable]
	class Output
	{
		public int requestId;
		public Text text;
		public string errorMessage;
	}

	AndroidJavaClass javaClass;
	Text result;
	int waitingRequestId; // int.MinValue以外なら待ってる途中
	int nextRequestId;

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
Debug.Log("MLKitWrapper.OnComplete: json\n" + outputJson);

			var output = JsonUtility.FromJson<Output>(outputJson);
			if (output.requestId == waitingRequestId) // ID違えば無視
			{
				if (string.IsNullOrEmpty(output.errorMessage))
				{
					result = output.text;
					ErrorMessage = null;
Debug.LogError("MLKitWrapper.OnComplete: Success " + output.text.textBlocks.Length + " " + output.text.text);
				}
				else
				{
					result = null;
					ErrorMessage = output.errorMessage;
Debug.LogError("MLKitWrapper.OnComplete: Error " + ErrorMessage);
				}
			}
		}
		catch (System.Exception e)
		{			
			Debug.LogException(e);
		}
		waitingRequestId = int.MinValue;
	}

	IEnumerator CoOnCompleteDummy()
	{
		yield return new WaitForSeconds(0.25f);
		Debug.Log("MLKitWrapper.OnComplete(DUMMY)");
		ErrorMessage = null;
		waitingRequestId = int.MinValue;

		var boundingBox = new Rect() { bottom = 432 - 8, left = 8, right = 768 - 8, top = 8 };

		var cornerPoints = new Point[4];
		cornerPoints[0] = new Point() { x = 8, y = 8 };
		cornerPoints[1] = new Point() { x = 768 - 8, y = 8 };
		cornerPoints[2] = new Point() { x = 768 - 8, y = 432 - 8 };
		cornerPoints[3] = new Point() { x = 8, y = 432 - 8 };

		result = new Text();
		result.text = "HOGE";
		result.textBlocks = new TextBlock[1];

		var textBlock = new TextBlock();
		textBlock.boundingBox = boundingBox;
		textBlock.cornerPoints = cornerPoints;
		textBlock.recognizedLanguage = "English";
		textBlock.text = "HOGE";
		textBlock.lines = new Line[1];
		result.textBlocks[0] = textBlock;

		var line = new Line();
		line.boundingBox = boundingBox;
		line.cornerPoints = cornerPoints;
		line.recognizedLanguage = "English";
		line.text = "HOGE";
		line.elements = new Element[1];
		textBlock.lines[0] = line;

		var element = new Element();
		element.boundingBox = boundingBox;
		element.cornerPoints = cornerPoints;
		element.recognizedLanguage = "English";
		element.text = "HOGE";
		line.elements[0] = element;
	}
}
