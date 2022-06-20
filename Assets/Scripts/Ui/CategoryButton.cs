using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
	[SerializeField] string description;
	[SerializeField] QuestionSubScene.Operation operation;
	[SerializeField] int op0min;
	[SerializeField] int op0max;
	[SerializeField] int op1min;
	[SerializeField] int op1max;
	[SerializeField] int answerMin;
	[SerializeField] int answerMax;
	[SerializeField] bool invertOperation;
	[SerializeField] Button button;

	public Button Button { get => button; }
	public QuestionSubScene.Settings MakeSettings()
	{
		return new QuestionSubScene.Settings(
			description,
			operation,
			op0min,
			op0max,
			op1min,
			op1max,
			answerMin,
			answerMax,
			invertOperation);
	}
}
