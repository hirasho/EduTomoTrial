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

				// 新しい順にソートするためにSortedDictionaryを利用
				var sortedSessions = new SortedDictionary<System.DateTime, SessionData>();
				for (var i = 0; i < latest.sessions.Count; i++)
				{
					var session = latest.sessions[i];
					var time = new System.DateTime();
					System.DateTime.TryParse(session.time, out time);
					time = time.AddTicks(i); // かぶらないようにするための安全策(ファイルには秒までしか書いていない)
					sortedSessions.Add(time, session);
				}
				
				foreach (var pair in sortedSessions)
				{
					var date = pair.Key;
					var session = pair.Value;
					var line = string.Format("{0:00}/{1:00}\t{2:00}:{3:00}\t{4}\t{5}問/{6:F0}秒(平均{7:F1}秒)\n",
						date.Month,
						date.Day,
						date.Hour,
						date.Minute, 
						session.description, 
						session.problemCount,
						session.duration,
						session.averageDuration);
					sb.Insert(0, line); // 逆順化
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
