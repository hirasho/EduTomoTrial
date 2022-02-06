using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crane : MonoBehaviour
{
	[SerializeField] ConfigurableJoint joint;

	public void Grab(Rigidbody objectRigidbody, Vector3 grabPoint)
	{
		transform.position = grabPoint;
		joint.connectedBody = objectRigidbody;
		var lp = objectRigidbody.transform.InverseTransformPoint(grabPoint);
		joint.connectedAnchor = lp;
	}

	public void Release()
	{
		joint.connectedBody = null;		
	}

	public void SetPosition(Vector3 position)
	{
		transform.position = position;
	}
}
