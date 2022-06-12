using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEraserEventReceiver
{
	void OnEraserDown();
	void OnEraserUp();
	void OnEraserHitLine(Line line);
}
