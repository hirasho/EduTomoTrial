using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEraserEventReceiver
{
	void OnEraserDown(int pointerId);
	void OnEraserUp(int pointerId);
	void OnEraserHitLine(Line line);
}
