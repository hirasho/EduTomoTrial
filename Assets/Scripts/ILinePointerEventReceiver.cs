using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILinePointerEventReceiver
{
	void OnLineDown(Line line);
	void OnLineUp();
}
