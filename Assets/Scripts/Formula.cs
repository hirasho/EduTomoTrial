using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Formula : MonoBehaviour
{
	[SerializeField] AnswerZone[] answerZones;
	[SerializeField] Text formulaText0;
	[SerializeField] Text formulaText1;

	public AnswerZone[] AnswerZones { get => answerZones; }
	public void SetFormulaText(string text0, string text1)
	{
		if (formulaText0 != null)
		{
			formulaText0.text = text0;
		}

		if (formulaText1 != null)
		{
			formulaText1.text = text1;
		}
	}
}
