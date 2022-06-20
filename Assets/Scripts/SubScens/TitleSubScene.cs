using UnityEngine;
using UnityEngine.UI;

public class TitleSubScene : SubScene
{
	[SerializeField] CategoryButton[] buttons;
	[SerializeField] Button freeDrawButton;
	[SerializeField] Button logButton;
	[SerializeField] Button settingsButton;

	public void ManualStart(Main main)
	{
		this.main = main;

		foreach (var button in buttons)
		{
			button.Button.onClick.AddListener(() =>
			{
				questionSettings = button.MakeSettings();
			});
		}

		freeDrawButton.onClick.AddListener(() =>
		{
			toFreeDraw = true;	
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
		else if (toFreeDraw)
		{
			var subScene = SubScene.Instantiate<FreeDrawSubScene>(transform.parent);
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
	bool toFreeDraw;
}
