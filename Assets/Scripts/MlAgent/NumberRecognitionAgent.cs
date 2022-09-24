using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class NumberRecognitionAgent : Agent
{
	public List<int> GuessedNumbers { get; private set; }

	public void ManualStart()
	{
		GuessedNumbers = new List<int>();
	}

	public void ClearResult()
	{
		GuessedNumbers.Clear();
	}

	public void Guess()
	{
		base.RequestDecision();
	}
	
	public override void OnActionReceived(ActionBuffers actionBuffers)
	{
		var guessed = actionBuffers.DiscreteActions[0];
		if (guessed < 10)
		{
			GuessedNumbers.Add(guessed);
		}
		EndEpisode();
	}

	// ヒューリスティックモードの行動決定時に呼ばれる
	public override void Heuristic(in ActionBuffers actionsOut)
	{
		var actions = actionsOut.DiscreteActions;
		actions[0] = Random.Range(0, 11);
	}

	// non public ----
	RenderTexture renderTexture;
}
