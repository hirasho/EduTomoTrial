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
		int operand0Digits, 
		int operand1Digits, 
		int answerMaxDigits, 
		string description,
		string userName,
		DateTime birthday)
	{
		this.time = DateTime.Now.ToString();
		this.operand0Digits = operand0Digits;
		this.operand1Digits = operand1Digits;
		this.answerMaxDigits = answerMaxDigits;
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
	public int operand0Digits;
	public int operand1Digits;
	public int answerMaxDigits;
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
