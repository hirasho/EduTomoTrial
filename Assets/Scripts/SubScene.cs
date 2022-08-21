using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SubScene : MonoBehaviour
{
	public static T Instantiate<T>(Transform root) where T: SubScene
	{
		T ret = null;
		var path = "Prefabs/SubScenes/" + typeof(T).Name;
		var prefab = Resources.Load<T>(path);
		if (prefab == null)
		{
			Debug.LogError("SubScene.Instantiate: " + path + " not found.");
		}
		else
		{
			ret = Instantiate(prefab, root, false);
		}
		return ret;
	}

	public abstract SubScene ManualUpdate(float deltaTime);

	public virtual void OnPointerDown(int pointerId)
	{		
	}

	public virtual void OnPointerUp(int pointerId)
	{		
	}

	public virtual void OnTextRecognitionComplete(IReadOnlyList<TextRecognizer.Word> words)
	{
	}
}
