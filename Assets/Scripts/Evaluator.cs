using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Evaluator
{
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

	public class EvaluatedLetter
	{
		public Letter srcLetter;
		public string numberText; // 数値に解釈されたもの
		public bool correct;
	}

	public static IList<Word> ExtractWords(VisionApi.BatchAnnotateImagesResponse batchResponses)
	{
		var ret = new List<Word>();
		foreach (var response in batchResponses.responses)
		{
			ProcessTextAnnotation(response.fullTextAnnotation, ret);
		}
		return ret;
	}

	public static bool EvaluateWord(List<Evaluator.EvaluatedLetter> lettersOut, Word word, double correctValue)
	{
		var ret = true;
		var answerText = correctValue.ToString();

		// 逆順マッチ。大抵1の位から書いて行くから。
		var answerIndex = answerText.Length - 1;
		var writtenIndex = word.letters.Count - 1;
		var matchCount = 0;
		lettersOut.Clear();
		while (lettersOut.Count < word.letters.Count)
		{
			lettersOut.Add(null);
		}

		while (writtenIndex >= 0)
		{
			var letter = word.letters[writtenIndex];
			var evaluatedLetter = new EvaluatedLetter();
			lettersOut[writtenIndex] = evaluatedLetter;
			evaluatedLetter.srcLetter = letter;
			evaluatedLetter.numberText = ReadNumber(letter.text);
			evaluatedLetter.correct = false;

			var ca = (answerIndex >= 0) ? answerText[answerIndex] : '\0';
			var cw = evaluatedLetter.numberText[0];
			if (cw != '?')
			{
				if (ca == cw)
				{
					evaluatedLetter.correct = true;
					matchCount++;
				}
				else
				{
					ret = false;
				}
				answerIndex--;
			}
			writtenIndex--;
		}

		if (answerText.Length != matchCount) // 長さが違えば違う
		{
			ret = false;
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
			ProcessParagraphs(paragraph, wordsOut);
		}
	}

	static void ProcessParagraphs(VisionApi.Paragraph paragraph, List<Word> wordsOut)
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

	public static string ReadNumber(string text)
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
