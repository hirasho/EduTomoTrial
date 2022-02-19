using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

public class VisionApi
{
	[Serializable]
	public class RequestBody
	{
		public List<AnnotateImageRequest> requests;
	}

	[Serializable]
	public class AnnotateImageRequest
	{
		public Image image;
		public List<Feature> features;
		//public string imageContext;
	}

	[Serializable]
	public class Image
	{
		public string content;
		//public ImageSource source;
	}

	[Serializable]
	public class ImageSource
	{
		public string gcsImageUri;
	}

	[Serializable]
	public class Feature
	{
		public string type;
		public int maxResults;
	}

	public enum FeatureType 
	{
		TYPE_UNSPECIFIED,
		FACE_DETECTION,
		LANDMARK_DETECTION,
		LOGO_DETECTION,
		LABEL_DETECTION,
		TEXT_DETECTION,
		DOCUMENT_TEXT_DETECTION,
		SAFE_SEARCH_DETECTION,
		IMAGE_PROPERTIES
	}

	[Serializable]
	public class ImageContext
	{
		public LatLongRect latLongRect;
		public string languageHints;
	}

	[Serializable]
	public class LatLongRect
	{
		public LatLng minLatLng;
		public LatLng maxLatLng;
	}

	[Serializable]
	public class LatLng
	{
		public float latitude;
		public float longitude;
	}

	[Serializable]
	public class BatchAnnotateImagesResponse {
		public List<AnnotateImageResponse> responses;
	}

	[Serializable]
	public class AnnotateImageResponse {
		public List<EntityAnnotation> landmarkAnnotations;
		public List<EntityAnnotation> logoAnnotations;
		public List<EntityAnnotation> labelAnnotations;
		public List<EntityAnnotation> textAnnotations;
		public EntityAnnotation fullTextAnnotation;
		public State error;

		// まだある
	}

	[Serializable]
	public class State
	{
		public long code;
		public string message;
		// まだある
	}

	[Serializable]
	public class EntityAnnotation {
		public string mid;
		public string locale;
		public string description;
		public double score;
		public double topicality;
		public BoundingPoly boundingPoly;
		public List<LocationInfo> locations;
		public List<Property> properties;
	}

	[Serializable]
	public class BoundingPoly {
		public List<Vertex> vertices;
	}

	[Serializable]
	public class Vertex {
		public float x;
		public float y;
	}

	[Serializable]
	public class LocationInfo {
		LatLng latLng;
	}

	[Serializable]
	public class Property {
		string name;
		string value;
	}

	public BatchAnnotateImagesResponse Response { get; private set; }
	public bool Requested { get; private set; }

	public VisionApi(string apiKey)
	{
		this.apiKey = apiKey;
	}

	public bool IsDone()
	{
		var ret = false;
		if (webRequest == null)
		{
			ret = true;
		}
		else
		{
			if (webRequest.isDone)
			{
				ret = true;
				if (!string.IsNullOrEmpty(webRequest.error)) 
				{
					Debug.LogError("WebRequestError: " + webRequest.responseCode + " : " + webRequest.error);
				}
				else 
				{
//					Debug.Log("ResponseCode: " + webRequest.responseCode + " size=" + webRequest.downloadHandler.data.Length);
#if UNITY_EDITOR
System.IO.File.WriteAllText("response.json", webRequest.downloadHandler.text);
#endif
					// 成功時の処理
					Response = JsonUtility.FromJson<BatchAnnotateImagesResponse>(webRequest.downloadHandler.text);
				}
				webRequest.Dispose();
				webRequest = null;
			}
		}
		return ret;
	}

	public void Complete()
	{
		Debug.Assert(IsDone());
		webRequest = null;	
		Requested = false;
	}

	public void Abort()
	{
		if (webRequest != null)
		{
			webRequest.Abort();
			webRequest = null;
		}
		Requested = false;
	}

	public void Request(Texture2D readableTexture2d)
	{
		Requested = true;
		var jpg = readableTexture2d.EncodeToJPG();
#if UNITY_EDITOR
System.IO.File.WriteAllBytes("ss.jpg", jpg);
#endif
		var base64Image = Convert.ToBase64String(jpg);

		var url = "https://vision.googleapis.com/v1/images:annotate?key=" + apiKey;
		// requestBodyを作成
		var requests = new RequestBody();
		requests.requests = new List<AnnotateImageRequest>();

		var request = new AnnotateImageRequest();
		request.image = new Image();
		request.image.content = base64Image;

		request.features = new List<Feature>();
		var feature = new Feature();
		feature.type = FeatureType.DOCUMENT_TEXT_DETECTION.ToString();
		feature.maxResults = 10;
		request.features.Add(feature);

		requests.requests.Add(request);

		// JSONに変換
		var jsonRequestBody = JsonUtility.ToJson(requests, prettyPrint: true);
#if UNITY_EDITOR
System.IO.File.WriteAllText("request.json", jsonRequestBody);
#endif
		// ヘッダを"application/json"にして投げる
		webRequest = new UnityWebRequest(url, "POST");
		var postData = System.Text.Encoding.UTF8.GetBytes(jsonRequestBody);
		webRequest.uploadHandler = (UploadHandler) new UploadHandlerRaw(postData);
		webRequest.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
		webRequest.SetRequestHeader("Content-Type", "application/json");

		webRequest.SendWebRequest();
	}

	// non public ---------
	string apiKey;
	UnityWebRequest webRequest;
}
