using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleSubScene : SubScene
{
	[SerializeField] int defaultQuestionCount = 20;
	[SerializeField] Button countButton;
	[SerializeField] Button add11_2Button;
	[SerializeField] Button sub21_1Button;
	[SerializeField] Button invAdd11_2Button;
	[SerializeField] Button invSub21_1Button;
	[SerializeField] Button mul11Button;
	[SerializeField] Button mul21Button;
	[SerializeField] Button addSub33_3Button;

	public void ManualStart(Main main)
	{
		this.main = main;
		questionCount = defaultQuestionCount; // これどこから渡す?

		countButton.onClick.AddListener(OnClickCount);
		add11_2Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x+y=?", QuestionSubScene.Operation.Addition, questionCount, 1, 1, 2, 2, false, false);
		});
		sub21_1Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xx-y=?", QuestionSubScene.Operation.Subtraction, questionCount, 2, 1, 1, 1, false, false);
		});
		invAdd11_2Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x+?=yy", QuestionSubScene.Operation.Addition, questionCount, 1, 1, 2, 2, false, true);
		});
		invSub21_1Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xx-?=y", QuestionSubScene.Operation.Subtraction, questionCount, 2, 1, 1, 1, false, true);
		});
		mul11Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x*y=??", QuestionSubScene.Operation.Multiplication, questionCount, 1, 1, 1, 2, false, false);
		});
		mul21Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xx*y=???", QuestionSubScene.Operation.Multiplication, questionCount, 2, 1, 2, 3, false, false);
		});
		addSub33_3Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xxx+-yyy=???", QuestionSubScene.Operation.AddAndSub, questionCount, 3, 3, 3, 3, false, false);
		});
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		SubScene ret = null;
		if (questionSettings != null)
		{
			var subScene = SubScene.Instantiate<QuestionSubScene>(transform.parent);
			subScene.ManualStart(
				main,
				questionSettings);
			ret = subScene;
		}
		return ret;
	}

	// non public ---------
	Main main;
	QuestionSubScene.Settings questionSettings;
	int questionCount;

	void OnClickCount()
	{
		// TODO:
	}
}
