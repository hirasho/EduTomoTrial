//#define DUMMY_MODE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLKitWrapper : MonoBehaviour
{
	public const int InvalidRequestId = int.MinValue;

	[SerializeField] int parallelCount = 2;

	[System.Serializable]
	public class Text
	{
		public string text;
		public TextBlock[] textBlocks;
		public int requestId;
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

	public bool Implemented
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

	// キューが埋まっているか
	public bool IsFull()
	{
		EnsureInitialize();
		return (FindEmptyRequestIndex() < 0);
	}

	// IDがInvalidなら何でもいいから返す
	public Text GetResult(bool resetOnGet, int requestId = InvalidRequestId)
	{
		EnsureInitialize();

		Text ret = null;
		foreach (var request in requests)
		{
			if (request.IsDone())
			{
				if ((request.requestId == requestId) || requestId == InvalidRequestId)
				{
					ret = request.output.text;
					ret.requestId = request.requestId;
					if (resetOnGet)
					{
Debug.Log("GetResult : " + resetOnGet + " " + request.requestId);
						request.Reset();
					}
					break;
				}
			}
		}
		return ret;
	}

	// 戻り値がInvalidRequestIdなら失敗している。一杯の時に勝手に止めたりはしない。
	public int RecognizeText(int width, int height, IReadOnlyList<Color32> pixels)
	{
		EnsureInitialize();

		Request newRequest = null;
		var newRequestId = InvalidRequestId;
		if (requests.Count < parallelCount)
		{
			newRequest = new Request();
			requests.Add(newRequest);
Debug.Log("Enqueue " + requests.Count);
		}
		else
		{
			var vacantIndex = FindEmptyRequestIndex();
			if (vacantIndex >= 0)
			{
				newRequest = requests[vacantIndex];
			}
		}

		if (newRequest == null)
		{
			return newRequestId;
		}

//Debug.Log("MLKit Recognize: " + width + "x" + height + " pixelCount=" + pixels.Count + " id=" + nextRequestId);

		var input = new Input();
		input.width = width;
		input.height = height;
		input.pixels = new int[pixels.Count];
		input.requestId = nextRequestId;
		newRequest.requestId = nextRequestId;

		newRequestId = nextRequestId;
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

		try
		{
			// jsonにすると遅いので、stringにバイナリデータ詰める

			string serializedInput = null;
#if true
			serializedInput = input.Serialize();
#else
			var json = JsonUtility.ToJson(input, true);
//Debug.Log("MLKit input jsonSize=" + json.Length + " req=" + Requested);
#if UNITY_EDITOR
System.IO.File.WriteAllText("mlkitInput.json", json);
#endif
#endif
#if DUMMY_MODE
			StartCoroutine(CoOnCompleteDummy(nextRequestId));
#else
			javaClass.CallStatic("recognizeText", gameObject.name, serializedInput);
#endif
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}

		nextRequestId++;
		if (nextRequestId == int.MinValue) // 来ないと思うが巻き戻った時のための対処
		{
			nextRequestId++;
		}

		return newRequestId;
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

	class Request
	{
		public Request()
		{
			Reset();
		}

		public void SetResult(Output output)
		{
			this.output = output;
		}

		public bool IsDone()
		{
			var ret = false;
			if (requestId != InvalidRequestId)
			{
				if (output != null)
				{
					ret = true;
				}
			}
			return ret;
		}

		public void Reset()
		{
			requestId = InvalidRequestId;
			output = null;
		}
		public Output output;
		public int requestId;
	}

	AndroidJavaClass javaClass;
	List<Request> requests;
	int nextRequestId; // 20億使い果たしたらバグります

	int FindEmptyRequestIndex()
	{
		var ret = int.MinValue;
		// 数が足りなければ足してそれ返す
		if (requests.Count < parallelCount)
		{
			var request = new Request();
			ret = requests.Count;
			requests.Add(request);
		}
		else
		{
			for (var i = 0; i < requests.Count; i++)
			{
				var request = requests[i];
				if (request.requestId == InvalidRequestId)
				{
					ret = i;
					break;
				}
			}
		}
		return ret;
	}


	Request FindRequest(int requestId)
	{
		var index = FindRequestIndex(requestId);
		Request ret = null;
		if (index >= 0)
		{
			ret = requests[index];
		}
		return ret;
	}

	int FindRequestIndex(int requestId)
	{
//Debug.Log("Find id=" + requestId + " reqs=" + requests.Count);
		var ret = int.MinValue;
		for (var i = 0; i < requests.Count; i++)
		{
			var request = requests[i];
//Debug.Log("\t" + i + " " + request.requestId);
			if (request.requestId == requestId)
			{
				ret = i;
				break;
			}
		}
		return ret;
	}

	void EnsureInitialize()
	{
		if (requests == null)
		{
			requests = new List<Request>();
		}

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
 
	// JAVAから呼ばれるので参照はない
	void OnComplete(string outputJson) 
	{
Debug.Log("MLKitWrapper.OnComplete: json\n" + outputJson);
		Output output = null;
		try
		{
			output = JsonUtility.FromJson<Output>(outputJson);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		
		if (output != null)
		{
			var request = FindRequest(output.requestId);
			if (request != null)
			{
				request.SetResult(output);
			}
		}
	}

	IEnumerator CoOnCompleteDummy(int requestId)
	{
		yield return new WaitForSeconds(0.25f);
		Debug.Log("MLKitWrapper.OnComplete(DUMMY)");

		var request = FindRequest(requestId);
		if (request != null)
		{
			var boundingBox = new Rect() { bottom = 432 - 8, left = 8, right = 768 - 8, top = 8 };

			var cornerPoints = new Point[4];
			cornerPoints[0] = new Point() { x = 8, y = 8 };
			cornerPoints[1] = new Point() { x = 768 - 8, y = 8 };
			cornerPoints[2] = new Point() { x = 768 - 8, y = 432 - 8 };
			cornerPoints[3] = new Point() { x = 8, y = 432 - 8 };

			var text = new Text();
			text.text = "HOGE";
			text.textBlocks = new TextBlock[1];

			var textBlock = new TextBlock();
			textBlock.boundingBox = boundingBox;
			textBlock.cornerPoints = cornerPoints;
			textBlock.recognizedLanguage = "English";
			textBlock.text = "HOGE";
			textBlock.lines = new Line[1];
			text.textBlocks[0] = textBlock;

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

			var output = new Output();
			output.text = text;

			request.SetResult(output);
		}
	}
}
