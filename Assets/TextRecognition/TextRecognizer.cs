using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextRecognizer : MonoBehaviour
{
	[SerializeField] MLKitWrapper mlKit;

	public class Word
	{
		public List<Letter> letters;
		public Vector2 boundsMin;
		public Vector2 boundsMax;
		public string text;
	}

	public class Letter
	{
		public List<Vector2> vertices;
		public string text;
	}

	public bool Requested { get; private set; }

	public void ManualStart(string visionApiKey)
	{
		if (!string.IsNullOrEmpty(visionApiKey))
		{
			visionApi = new VisionApi.Client(visionApiKey);
		}
	}

	public bool IsDone()
	{
		var ret = false;
		if (requestIsMlkit)
		{
			ret = mlKit.IsDone();
		}
		else
		{
			ret = visionApi.IsDone();
		}
		return ret;
	}

	public void Abort()
	{
		if (requestIsMlkit)
		{
			mlKit.Abort();
		}
		else
		{
			visionApi.Abort();
		}
		Requested = false;
	}

	public void ClearDiffImage()
	{
		if (prevPixels != null)
		{
			var c = new Color32(255, 255, 255, 0); // 白
			for (var i = 0; i < prevPixels.Length; i++)
			{
				prevPixels[i] = c;
			}
		}
	}

	public IReadOnlyList<Word> GetResult()
	{
		List<Word> ret = null;
		if (requestIsMlkit)
		{
			if (mlKit.IsDone())
			{
Debug.LogError("TextRecognizer.GetResult: mlKit.enabled=" + mlKit.Enabled + " " + mlKit.ErrorMessage);
				var result = mlKit.GetResult();
				if ((result != null) && (result.textBlocks != null))
				{
					ret = new List<Word>();
Debug.LogError("TextRecognizer.GetResult: textBlocks=" + result.textBlocks.Length);
					foreach (var block in result.textBlocks)
					{
						ProcessTextBlock(block, ret);
					}
				}
			}
		}
		else
		{
			if (visionApi.IsDone())
			{
				ret = new List<Word>();
				foreach (var response in visionApi.Response.responses)
				{
					ProcessTextAnnotation(response.fullTextAnnotation, ret);
				}
			}
		}
		Requested = false;
		return ret;
	} 

	public bool Request(Texture2D texture, IReadOnlyList<RectInt> rects)
	{
		requestIsMlkit = false;
		// dirty判定
		var dirty = false;
		var pixels = texture.GetPixels32();
		var width = texture.width;
		var height = texture.height;
		// なければor解像度違えば作って真っ白で埋める
		if ((prevPixels == null) || (pixels.Length != prevPixels.Length))
		{
			prevPixels = new Color32[width * height];
			prevWidth = width;
			ClearDiffImage();
		}

		if ((rects == null) || (rects.Count == 0))
		{
			dirty = FindDiff(
				prevPixels, 
				prevWidth, 
				pixels, 
				width, 
				new RectInt(0, 0, width, height));
		}
		else
		{
			foreach (var rect in rects)
			{
				if (FindDiff(prevPixels, prevWidth, pixels, width, rect))
				{
					dirty = true;
					break;
				} 
			}
		}
		prevPixels = pixels;
		prevWidth = width;

		if (!dirty)
		{
			return false;
		}

		if (Requested && !IsDone()) // 前のが終わってないので止める
		{
			Abort();
		}
		Requested = true;

		var ret = false;
		if (mlKit.Enabled)
		{
			ret = mlKit.RecognizeText(width, height, pixels);
			requestIsMlkit = true;
		}

		if (!ret && (visionApi != null))
		{
			ret = visionApi.Request(texture);
		}
		return ret;
	}

	// non public -----
	VisionApi.Client visionApi;
	Color32[] prevPixels;
	int prevWidth;
	bool requestIsMlkit;

	static bool FindDiff(Color32[] texels0, int width0, Color32[] texels1, int width1, RectInt rect)
	{
		var ret = false;
		for (var y = rect.y; y < (rect.y + rect.height); y++)
		{
			for (var x = rect.x; x < (rect.x + rect.width); x++)
			{
				var c0 = texels0[(y * width0) + x];
				var c1 = texels1[(y * width1) + x];
				if ((c0.r != c1.r) || (c0.g != c1.g) || (c0.b != c1.b))
				{
					ret = true;
					break;
				}
			}
		}
		return ret;
	}

	static void ProcessTextAnnotation(VisionApi.TextAnnotation textAnnotation, List<Word> wordsOut)
	{
		if (textAnnotation.pages != null)
		{
			foreach (var page in textAnnotation.pages)
			{
				ProcessPage(page, wordsOut);
			}
		}
	}

	static void ProcessPage(VisionApi.Page page, List<Word> wordsOut)
	{
		foreach (var block in page.blocks)
		{
			ProcessBlock(block, wordsOut);
		}
	}

	static void ProcessBlock(VisionApi.Block block, List<Word> wordsOut)
	{
		foreach (var paragraph in block.paragraphs)
		{
			ProcessParagraph(paragraph, wordsOut);
		}
	}

	static void ProcessParagraph(VisionApi.Paragraph paragraph, List<Word> wordsOut)
	{
		foreach (var word in paragraph.words)
		{
			var readWord = ProcessWord(word);
			if (readWord != null)
			{
				wordsOut.Add(readWord);
			}
		}
	}

	static Word ProcessWord(VisionApi.Word word)
	{
		var ret = new Word();
		ret.letters = new List<Letter>();
		var min = Vector2.one * float.MaxValue;
		var max = -min;
		foreach (var symbol in word.symbols)
		{
			var letter = ProcessSymbol(symbol);
			ret.letters.Add(letter);
			ret.text += letter.text;
			foreach (var vertex in letter.vertices)
			{
				min = Vector2.Min(min, vertex);
				max = Vector2.Max(max, vertex);
			}
		}
		ret.boundsMin = min;
		ret.boundsMax = max;
		return ret;
	}

	static Letter ProcessSymbol(VisionApi.Symbol symbol)
	{
		var ret = new Letter();
		ret.vertices = new List<Vector2>();
		// 頂点抽出
		var srcVertices = symbol.boundingBox.vertices;
		for (var i = 0; i < srcVertices.Count; i++)
		{
			var srcV = srcVertices[i];
			ret.vertices.Add(new Vector2(srcV.x, srcV.y));
		}

		ret.text = symbol.text;
		return ret;
	}

	static void ProcessTextBlock(MLKitWrapper.TextBlock textBlock, List<Word> wordsOut)
	{
		foreach (var line in textBlock.lines)
		{
			ProcessLine(line, wordsOut);
		}
	}

	static void ProcessLine(MLKitWrapper.Line line, List<Word> wordsOut)
	{
		foreach (var element in line.elements)
		{
			ProcessElement(element, wordsOut);
		}
	}

	static void ProcessElement(MLKitWrapper.Element element, List<Word> wordsOut)
	{
		var word = new Word();
		word.letters = new List<Letter>();
		word.boundsMin = new Vector2(element.boundingBox.left, element.boundingBox.top);
		word.boundsMax = new Vector2(element.boundingBox.right, element.boundingBox.bottom);
		Debug.Assert(word.boundsMin.x < word.boundsMax.x);
		Debug.Assert(word.boundsMin.y < word.boundsMax.y);
		var text = element.text;
		word.text = text;

		// Letterはここでは雑に分割する。後でLetterなしにするかもしれない
		var width = word.boundsMax.x - word.boundsMin.x;
		for (var i = 0; i < text.Length; i++)
		{
			var letter = new Letter();
			letter.vertices = new List<Vector2>();
			var x0 = word.boundsMin.x + (width * (float)i / (float)text.Length);
			var x1 = word.boundsMin.x + (width * (float)(i + 1) / (float)text.Length);
			letter.vertices.Add(new Vector2(x0 + 2, word.boundsMin.y + 2));
			letter.vertices.Add(new Vector2(x1 - 2, word.boundsMin.y + 2));
			letter.vertices.Add(new Vector2(x1 - 2, word.boundsMax.y - 2));
			letter.vertices.Add(new Vector2(x0 + 2, word.boundsMax.y - 2));
			letter.text = new string(text[i], 1);
			word.letters.Add(letter);
		}
		wordsOut.Add(word);
	}
}
