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
	[SerializeField] Button backButton;

	public void ManualStart(Main main)
	{
		this.main = main;
		backButton.onClick.AddListener(() => { toTitle = true; });

		var sd = main.SaveData;
		minProblemCountSlider.ManualStart(OnChange, sd.minProblemCount);
		maxProblemCountSlider.ManualStart(OnChange, sd.maxProblemCount);
		timeSlider.ManualStart(OnChange, sd.timeMinute);
		saveTimer = saveIntervalSecond;
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		minProblemCountSlider.ManualUpdate(deltaTime);
		maxProblemCountSlider.ManualUpdate(deltaTime);
		timeSlider.ManualUpdate(deltaTime);

		SubScene ret = null;
		if (toTitle)
		{
			var subScene = SubScene.Instantiate<TitleSubScene>(transform.parent);
			subScene.ManualStart(main);
			ret = subScene;
			Save();
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

	void Save()
	{
		if (dirty)
		{
			var sd = main.SaveData;
			sd.maxProblemCount = maxProblemCountSlider.IntValue;
			sd.minProblemCount = minProblemCountSlider.IntValue;
			sd.timeMinute = timeSlider.IntValue;
			sd.Save();
			dirty = false;
		}
		saveTimer = saveIntervalSecond;
	}

	void OnChange()
	{
		dirty = true;
	}
}
