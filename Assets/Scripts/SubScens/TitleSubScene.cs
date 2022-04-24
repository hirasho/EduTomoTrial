using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleSubScene : SubScene
{
	[SerializeField] Button countButton;
	[SerializeField] Button add11_2Button;
	[SerializeField] Button sub21_1Button;
	[SerializeField] Button invAdd11_2Button;
	[SerializeField] Button invSub21_1Button;
	[SerializeField] Button mul11Button;
	[SerializeField] Button mul21Button;
	[SerializeField] Button madd11Button;
	[SerializeField] Button addSub33_3Button;
	[SerializeField] Button logButton;
	[SerializeField] Button settingsButton;

	public void ManualStart(Main main)
	{
		this.main = main;

		countButton.onClick.AddListener(OnClickCount);
		add11_2Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x+y=?", QuestionSubScene.Operation.Addition, 1, 1, 2, 2, false, false);
		});
		sub21_1Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xx-y=?", QuestionSubScene.Operation.Subtraction, 2, 1, 1, 1, false, false);
		});
		invAdd11_2Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x+?=yy", QuestionSubScene.Operation.Addition, 1, 1, 2, 2, false, true);
		});
		invSub21_1Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xx-?=y", QuestionSubScene.Operation.Subtraction, 2, 1, 1, 1, false, true);
		});
		mul11Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x*y=??", QuestionSubScene.Operation.Multiplication, 1, 1, 1, 2, false, false);
		});
		mul21Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xx*y=???", QuestionSubScene.Operation.Multiplication, 2, 1, 2, 3, false, false);
		});
		madd11Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("x*?+y=zz", QuestionSubScene.Operation.Madd, 1, 1, 1, 2, false, true);
		});
		addSub33_3Button.onClick.AddListener(() =>
		{
			questionSettings = new QuestionSubScene.Settings("xxx+-yyy=???", QuestionSubScene.Operation.AddAndSub, 3, 3, 3, 3, false, false);
		});

		logButton.onClick.AddListener(() => { toLog = true; });
		settingsButton.onClick.AddListener(() => { toSettings = true; });
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		SubScene ret = null;
		if (toLog)
		{
			var subScene = SubScene.Instantiate<LogViewSubScene>(transform.parent);
			subScene.ManualStart(main);
			ret = subScene;
		}
		else if (toSettings)
		{
			var subScene = SubScene.Instantiate<SettingsSubScene>(transform.parent);
			subScene.ManualStart(main);
			ret = subScene;
		}
		else if (questionSettings != null)
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
	bool toLog;
	bool toSettings;

	void OnClickCount()
	{
		// TODO:
	}
}
