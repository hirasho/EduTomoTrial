using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsSubScene : SubScene
{
	[SerializeField] float saveIntervalSecond = 5f;
	[SerializeField] UiSlider minProblemCountSlider;
	[SerializeField] UiSlider maxProblemCountSlider;
	[SerializeField] UiSlider timeSlider;
	[SerializeField] Toggle allowZeroToggle;
	[SerializeField] Toggle showCubesToggle;
	[SerializeField] Toggle visionApiToggle;
	[SerializeField] Button backButton;
	[SerializeField] HiddenCommand hiddenCommand;

	public void ManualStart(Main main)
	{
		this.main = main;
		hiddenCommand.SetPassword(main.Keys.VisionApiUnlockCommand);
		backButton.onClick.AddListener(() => { toTitle = true; });

		var sd = main.SaveData;
		minProblemCountSlider.ManualStart(OnChange, sd.minProblemCount);
		maxProblemCountSlider.ManualStart(OnChange, sd.maxProblemCount);
		timeSlider.ManualStart(OnChange, sd.secondsPerProblem);
		allowZeroToggle.isOn = sd.allowZero;
		showCubesToggle.isOn = sd.showCubes;
		allowZeroToggle.onValueChanged.AddListener(OnChangeToggle);
		showCubesToggle.onValueChanged.AddListener(OnChangeToggle);
		if (sd.useVisionApi)
		{
			UnlockVisionApiToggle();
		}

		saveTimer = saveIntervalSecond;
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		// 隠しコマンド
		if (hiddenCommand.Unlocked)
		{
			UnlockVisionApiToggle();
			hiddenCommand.ClearUnlocked();
		}

		minProblemCountSlider.ManualUpdate(deltaTime);
		maxProblemCountSlider.ManualUpdate(deltaTime);
		if (minProblemCountSlider.IntValue > maxProblemCountSlider.IntValue)
		{
			maxProblemCountSlider.SetValue(minProblemCountSlider.IntValue);
		}
		timeSlider.ManualUpdate(deltaTime);

		SubScene ret = null;
		if (toTitle)
		{
			var subScene = SubScene.Instantiate<TitleSubScene>(transform.parent);
			subScene.ManualStart(main);
			ret = subScene;
			Save();
			main.ResetTextRecognizer();
		}
		else
		{
			saveTimer -= deltaTime;
			if (saveTimer <= 0f)
			{
				Save();
			}
		}
		return ret;
	}

	// non public ---------
	Main main;
	bool toTitle;
	bool dirty = false;
	float saveTimer;

	void UnlockVisionApiToggle()
	{
		visionApiToggle.gameObject.SetActive(true);
		visionApiToggle.isOn = main.SaveData.useVisionApi;
		visionApiToggle.onValueChanged.AddListener(OnChangeToggle);
	}

	void Save()
	{
		if (dirty)
		{
			var sd = main.SaveData;
			sd.maxProblemCount = maxProblemCountSlider.IntValue;
			sd.minProblemCount = minProblemCountSlider.IntValue;
			sd.secondsPerProblem = timeSlider.IntValue;
			sd.allowZero = allowZeroToggle.isOn;
			sd.showCubes = showCubesToggle.isOn;
			if (visionApiToggle.gameObject.activeInHierarchy)
			{
				sd.useVisionApi = visionApiToggle.isOn;
			}
			main.Save();
			dirty = false;
		}
		saveTimer = saveIntervalSecond;
	}

	void OnChange()
	{
		dirty = true;
	}

	void OnChangeToggle(bool unused)
	{
		OnChange();
	}
}
