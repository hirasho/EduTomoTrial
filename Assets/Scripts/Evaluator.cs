using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Evaluator
{
	public class Letter
	{
		public List<Vector2> vertices;
		public string text;
		public bool correct;
	}

	// 評価はしない。文字のリストを全部取ってくる
	public static IList<Letter> GetLetters(VisionApi.BatchAnnotateImagesResponse batchResponses)
	{
		var ret = new List<Letter>();
		foreach (var response in batchResponses.responses)
		{
			ProcessTextAnnotation(response.fullTextAnnotation, ret);
		}
		return ret;
	}
	
	public static IList<Letter> Evaluate(VisionApi.BatchAnnotateImagesResponse batchResponses, double answer, out bool correct)
	{
		correct = false;
		var ret = new List<Letter>();
		foreach (var response in batchResponses.responses)
		{
			var childCorrect = false;
			ProcessTextAnnotation(response.fullTextAnnotation, ret, out childCorrect, answer);
			if (childCorrect)
			{
				correct = true;
			}
		}
		return ret;
	}

	static void ProcessTextAnnotation(VisionApi.TextAnnotation textAnnotation, List<Letter> letters)
	{
		foreach (var page in textAnnotation.pages)
		{
			ProcessPage(page, letters);
		}
	}

	static void ProcessTextAnnotation(VisionApi.TextAnnotation textAnnotation, List<Letter> letters, out bool correct, double answer)
	{
//Debug.Log("\tTA");
		correct = false;
		foreach (var page in textAnnotation.pages)
		{
			var childCorrect = false;
			ProcessPage(page, letters, out childCorrect, answer);
			if (childCorrect)
			{
				correct = true;
			}
		}
	}

	static void ProcessPage(VisionApi.Page page, List<Letter> letters)
	{
		foreach (var block in page.blocks)
		{
			ProcessBlock(block, letters);
		}
	}

	static void ProcessPage(VisionApi.Page page, List<Letter> letters, out bool correct, double answer)
	{
//Debug.Log("\t\tPage");
		correct = false;
		foreach (var block in page.blocks)
		{
			var childCorrect = false;
			ProcessBlock(block, letters, out childCorrect, answer);
			if (childCorrect)
			{
				correct = true;
			}
		}
	}

	static void ProcessBlock(VisionApi.Block block, List<Letter> letters)
	{
		foreach (var paragraph in block.paragraphs)
		{
			ProcessParagraphs(paragraph, letters);
		}
	}

	static void ProcessBlock(VisionApi.Block block, List<Letter> letters, out bool correct, double answer)
	{
//Debug.Log("\t\t\tBlock");
		correct = false;
		foreach (var paragraph in block.paragraphs)
		{
			var childCorrect = false;
			ProcessParagraphs(paragraph, letters, out childCorrect, answer);
			if (childCorrect)
			{
				correct = true;
			}
		}
	}

	static void ProcessParagraphs(VisionApi.Paragraph paragraph, List<Letter> letters)
	{
		foreach (var word in paragraph.words)
		{
			ProcessWord(word, letters, readAsNumber: false);
		}
	}

	static void ProcessParagraphs(VisionApi.Paragraph paragraph, List<Letter> letters, out bool correct, double answer)
	{
		correct = true;
		var wordLetters = new List<Letter>();
		foreach (var word in paragraph.words)
		{
			ProcessWord(word, wordLetters, readAsNumber: true);
		}
/*
var str = "";
foreach (var letter in wordLetters)
{
	str += letter.text;
}	
Debug.Log("ProcessParagraphs: " + wordLetters.Count + " str=" + str);
*/
		var answerText = answer.ToString();
		// 逆順マッチ。大抵1の位から書いて行くから。
		var answerIndex = answerText.Length - 1;
		var writtenIndex = wordLetters.Count - 1;
		var matchCount = 0;
		while (writtenIndex >= 0)
		{
			var letter = wordLetters[writtenIndex];
			letter.correct = false;
			var ca = (answerIndex >= 0) ? answerText[answerIndex] : '\0';
			var cw = letter.text[0];
			if (cw != '?')
			{
				if (ca == cw)
				{
					letter.correct = true;
					matchCount++;
				}
				else
				{
					correct = false;
				}
				answerIndex--;
			}
			writtenIndex--;
		}

		foreach (var letter in wordLetters)
		{
			letters.Add(letter);
		}

		if (answerText.Length != matchCount) // 長さが違えば違う
		{
			correct = false;
		}
	}

	static void ProcessWord(VisionApi.Word word, List<Letter> letters, bool readAsNumber)
	{
		foreach (var symbol in word.symbols)
		{
			var letter = ProcessSymbol(symbol, readAsNumber);
			letters.Add(letter);
		}
	}

	static Letter ProcessSymbol(VisionApi.Symbol symbol, bool readAsNumber)
	{
		var letter = new Letter();
		letter.vertices = new List<Vector2>();
		// 頂点抽出
		var srcVertices = symbol.boundingBox.vertices;
		var dstVertices = new Vector3[srcVertices.Count];
		var center = Vector3.zero;
		var min = Vector3.one * float.MaxValue;
		var max = -min;
		for (var i = 0; i < srcVertices.Count; i++)
		{
			var srcV = srcVertices[i];
			letter.vertices.Add(new Vector2(srcV.x, srcV.y));
		}

		if (readAsNumber)
		{
			letter.text = ReadNumber(symbol.text);
		}
		else
		{
			letter.text = symbol.text;
		}
		return letter;
	}

	static string ReadNumber(string text)
	{
		var sb = new System.Text.StringBuilder();
		for (var i = 0; i < text.Length; i++)
		{
			var digit = TryReadDigit(text[i]);
			var c = (digit < 0) ? "?" : digit.ToString();
			sb.Append(c);
		}
		return sb.ToString();
	}

	static int TryReadDigit(char c)
	{
		var digit = -1;
		if ((c >= '0') && (c <= '9'))
		{
			digit = c - '0';
		}
		else if ((c == 'o') || (c == 'O') || (c == 'D'))
		{
			digit = 0;
		}
		else if ((c == '|') || (c == 'i') || (c == 'I') || (c == 'l') || (c == ')') || (c == '('))
		{
			digit = 1;
		}
		else if ((c == 's') || (c == 'S'))
		{
			digit = 5;
		}
		else if ((c == 'q') || (c == '។') || (c == 'a') || (c == '၄'))
		{
			digit = 9;
		}
		return digit;
	}
}
