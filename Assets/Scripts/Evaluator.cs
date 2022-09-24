using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Evaluator
{
	public class EvaluatedLetter
	{
		public TextRecognizer.Letter srcLetter;
		public string numberText; // 数値に解釈されたもの
		public bool correct;
	}

	public static bool EvaluateWord(List<Evaluator.EvaluatedLetter> lettersOut, TextRecognizer.Word word, double correctValue)
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

			var ca = (answerIndex >= 0) ? answerText[answerIndex] : ' ';
			var cw = evaluatedLetter.numberText[0];
//Debug.Log("E: " + evaluatedLetter.numberText + " " + evaluatedLetter.srcLetter + " " + cw);
//Debug.Log("Eval " + writtenIndex + " " + answerIndex + " " + ca + " <> " + cw + " " + ret + " " + matchCount);
			if (IsDigit(cw))
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
//Debug.Log("Eval Last " + answerText.Length + " <> " + matchCount + " ret= " + ret);
		return ret;
	}

	public static bool IsDigit(char c)
	{
		return ((c >= '0') && (c <= '9'));
	}

	public static string ReadNumber(string text)
	{
		var sb = new System.Text.StringBuilder();
		for (var i = 0; i < text.Length; i++)
		{
			var digit = TryReadDigit(text[i]);
			var c = (digit < 0) ? text : digit.ToString();
//Debug.Log("ReadNumber " + c + " <- " + text[i]);
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
		else if ((c == 'o') || (c == 'о') || (c == 'O') || (c == 'D'))
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
		else if ((c == 'б') || (c == 'ь'))
		{
			digit = 6;
		}
		else if ((c == 'ㄱ'))
		{
			digit = 7;
		}
		else if ((c == 'q') || (c == '។') || (c == 'a') || (c == '၄') || (c == '?'))
		{
			digit = 9;
		}
		return digit;
	}
}
