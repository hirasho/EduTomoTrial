using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class LogData
{
	public LogData()
	{
		this.createTime = DateTime.Now;
		sessions = new List<SessionData>();
	}
	public DateTime createTime;
	public List<SessionData> sessions;
}


[System.Serializable]
public class SessionData
{
	public SessionData(
		int operand0min, 
		int operand0max, 
		int operand1min, 
		int operand1max, 
		int answerMin,
		int answerMax, 
		string description,
		string userName,
		DateTime birthday)
	{
		this.time = DateTime.Now.ToString();
		this.operand0min = operand0min;
		this.operand0max = operand0max;
		this.operand1min = operand1min;
		this.operand1max = operand1max;
		this.answerMin = answerMin;
		this.answerMax = answerMax;
		this.description = description;
		this.problems = new List<ProblemData>();
		this.userName = userName;
		this.birthday = birthday.ToString();
	}

	public void AddProblemData(ProblemData problem)
	{
		problemCount++;
		problems.Add(problem);
		var now = DateTime.Now;
		try
		{
			duration = (float)((now - DateTime.Parse(time)).TotalSeconds);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
			duration = 0f;
		}
		averageDuration = duration / (float)problemCount;
	}

	public List<ProblemData> problems;
	public int problemCount;
	public string time;
	public float duration;
	public float averageDuration;
	public int operand0min;
	public int operand0max;
	public int operand1min;
	public int operand1max;
	public int answerMin;
	public int answerMax;
	public string description;
	public string userName;
	public string birthday;
}

[System.Serializable]
public class ProblemData
{
	public ProblemData(string text, float duration, int strokes, int erased)
	{
		this.text = text;
		this.duration = duration;
		this.strokes = strokes;
		this.erased = erased;
	}
	public string text;	
	public float duration;
	public int strokes;
	public int erased;
}
