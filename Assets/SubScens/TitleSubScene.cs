using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleSubScene : SubScene
{
	[SerializeField] Button startButton;
	[SerializeField] Toggle allowCarryBorrowToggle;
	[SerializeField] Toggle allowZeroToggle;
	[SerializeField] Toggle under1000Toggle;

	public void ManualStart(Main main)
	{
		this.main = main;
		startButton.onClick.AddListener(() => 
		{
			OnClickStart();
		});
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		return nextSubScene;
	}

	// non public ---------
	SubScene nextSubScene;
	Main main;

	void OnClickStart()
	{
		var scene = SubScene.Instantiate<QuestionSubScene>(transform.parent);
		scene.ManualStart(
			main, 
			QuestionSubScene.Operation.Addition, 
			allowZeroToggle.isOn,
			allowCarryBorrowToggle.isOn,
			under1000Toggle.isOn,
			questionCount: 20);
		nextSubScene = scene;
	}
}
