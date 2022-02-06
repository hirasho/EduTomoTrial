using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
	[SerializeField] AudioClip[] clips;
	[SerializeField] AudioSource source;

	public void Play(string name, float volumeDb = 0f)
	{
		AudioClip foundClip = null;
		foreach (var clip in clips)
		{
Debug.Log(clip.name + " <-> " + name);
			if (clip.name == name)
			{
				foundClip = clip;
				break;
			}
		}

		if (foundClip != null)
		{
			source.clip = foundClip;
			source.volume = ToLinearVolume(volumeDb);
			source.Play();
		}
	}

	// non public ------
	float ToLinearVolume(float db)
	{
		float ret = Mathf.Pow(10f, db / 20f);
		ret = Mathf.Clamp01(ret);
		return ret;
	}
}
