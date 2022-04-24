using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultSubScene : SubScene
{
	[SerializeField] Button toTitleButton;
	[SerializeField] Text timeText;

	public void ManualStart(Main main, float time, int questionCount)
	{
		this.main = main;
		var minF = time / 60f;
		var min = Mathf.FloorToInt(minF);
		var sec = time - (min * 60);
		var avg = time / (float)questionCount;
		timeText.text = string.Format("もんだいのかず:{0}\n{1}ふん{2}びょう!\n(平均{3}秒)", questionCount, min, sec.ToString("F0"), avg.ToString("F2"));
		toTitleButton.onClick.AddListener(() => 
		{
			OnClickToTitle();
		});
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		return nextSubScene;
	}

	// non public ---------
	SubScene nextSubScene;
	Main main;

	void OnClickToTitle()
	{
		var scene = SubScene.Instantiate<TitleSubScene>(transform.parent);
		scene.ManualStart(main);
		nextSubScene = scene;
	}
}
