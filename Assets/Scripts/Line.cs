using UnityEngine;
using UnityEngine.EventSystems;

public class Line : MonoBehaviour//, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] float minimumStep = 0.01f;
	[SerializeField] float baseY;
	[SerializeField] new LineRenderer renderer;
	[SerializeField] new MeshCollider collider;
	[SerializeField] LineMeshGenerator generator;

	public void ManualStart(ILinePointerEventReceiver eventReceiver, Camera camera, float lineWidth)
	{
		this.eventReceiver = eventReceiver;
		this.camera = camera;
//		renderer.startWidth = renderer.endWidth = lineWidth;
//		renderer.positionCount = 0;
		transform.localPosition = new Vector3(0f, baseY, 0f);
		prevPoint = Vector3.one * float.MaxValue;
		generator.Width = lineWidth;
	}

	public void ReplaceMaterial(Material material)
	{
//		renderer.sharedMaterial = material;
		generator.SetMaterial(material);
	}

	public void AddPoint(Vector3 p)
	{
		p.y = baseY;
		if (Vector3.Distance(prevPoint, p) > minimumStep)
		{
			prevPoint = p;
//			var index = renderer.positionCount;
//			renderer.positionCount++;
//			renderer.SetPosition(index, p);
			generator.AddPoint(p);
		}
	}

	public void GenerateCollider()
	{
		generator.UpdateMesh();
		collider.sharedMesh = generator.Mesh;
/*
		if (mesh == null)
		{
			mesh = new Mesh();
			mesh.name = "LineBaked";
		}
		renderer.BakeMesh(mesh, camera, false);
		collider.sharedMesh = mesh;
*/
	}
/*
	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventReceiver != null)
		{
			eventReceiver.OnLineDown(this);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (eventReceiver != null)
		{
			eventReceiver.OnLineUp();
		}
	}

	// こいつらないと引っぱった時に即upが来てしまう
	public void OnBeginDrag(PointerEventData eventData){}
	public void OnDrag(PointerEventData eventData){}
	public void OnEndDrag(PointerEventData eventData){}
*/
	// non public ------
	Mesh mesh;
	ILinePointerEventReceiver eventReceiver;
	new Camera camera;
	Vector3 prevPoint;
}
