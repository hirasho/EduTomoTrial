using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogViewSubScene : SubScene
{
	[SerializeField] Button backButton;
	[SerializeField] Text text;

	public void ManualStart(Main main)
	{
		this.main = main;
		backButton.onClick.AddListener(() => { toTitle = true; });

		LoadLog();
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		SubScene ret = null;
		if (toTitle)
		{
			var subScene = SubScene.Instantiate<TitleSubScene>(transform.parent);
			subScene.ManualStart(main);
			ret = subScene;
		}
		return ret;
	}

	// non public ---------
	Main main;
	bool toTitle;

	void LoadLog()
	{
		var path = SaveDataPathUtil.MakeFullPath("log_latest.json");
		if (System.IO.File.Exists(path))
		{
			try
			{
				var json = System.IO.File.ReadAllText(path);
				var latest = JsonUtility.FromJson<LogData>(json);
				var sb = new System.Text.StringBuilder();
				foreach (var session in latest.sessions)
				{
					var date = System.DateTime.Parse(session.time);
					sb.AppendLine(date.Month + "/" + date.Day + " " + date.Hour + ":" + date.Minute + " " + session.description + " " + session.duration.ToString("F0") + "秒/" + session.problemCount + "問(平均" + session.averageDuration.ToString("F1") + "秒)");
				}
				text.text = sb.ToString();
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

	}
}
