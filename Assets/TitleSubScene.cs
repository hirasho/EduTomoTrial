using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleSubScene : SubScene
{
	[SerializeField] Button startButton;

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
		scene.ManualStart(main);
		nextSubScene = scene;
	}
}
