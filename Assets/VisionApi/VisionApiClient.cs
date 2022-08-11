using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace VisionApi
{
	public class Client
	{
		public class ReadWord
		{
			public List<ReadLetter> letters;
			public string text;
		}

		public class ReadLetter
		{
			public List<Vector2> vertices;
			public string text;
		}

		public BatchAnnotateImagesResponse Response { get; private set; }
		public bool Requested { get; private set; }

		public Client(string apiKey)
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

		public void ClearDiffImage()
		{
			if (prevTexels != null)
			{
				var c = new Color32(255, 255, 255, 0); // 白
				for (var i = 0; i < prevTexels.Length; i++)
				{
					prevTexels[i] = c;
				}
			}
		}

		// テクスチャが変わってないとfalse返して終わる
		public bool Request(RenderTexture rt)
		{
			Graphics.SetRenderTarget(rt, 0);
			// 読み出し用テクスチャを生成して差し換え
			var texture2d = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
			texture2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), destX: 0, destY: 0);
			
			return Request(texture2d, null);
		}

		public bool Request(Texture2D readableImage, IReadOnlyList<RectInt> rects)
		{
			// dirty判定
			var dirty = false;
			var newTexels = readableImage.GetPixels32();
			// なければ作って真っ白で埋める
			if (prevTexels == null)
			{
				prevTexels = new Color32[readableImage.width * readableImage.height];
				ClearDiffImage();
			}
			else if (newTexels.Length != prevTexels.Length)
			{
				prevTexels = new Color32[readableImage.width * readableImage.height];
				ClearDiffImage();
			}

			else if ((rects == null) || (rects.Count == 0))
			{
				dirty = FindDiff(
					prevTexels, 
					prevWidth, 
					newTexels, 
					readableImage.width, 
					new RectInt(0, 0, readableImage.width, readableImage.height));
			}
			else
			{
				foreach (var rect in rects)
				{
					if (FindDiff(prevTexels, prevWidth, newTexels, readableImage.width, rect))
					{
						dirty = true;
						break;
					} 
				}
			}
			prevTexels = newTexels;
			prevWidth = readableImage.width;

			if (!dirty)
			{
				return false;
			}


			if (!IsDone()) // 前のが終わってないので止める
 			{
				Abort();
			}

			Requested = true;
			var jpg = readableImage.EncodeToJPG();
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
			return true;
		}

		public IList<ReadWord> GetResult()
		{
			if (!IsDone())
			{
				return null;
			}

			var ret = new List<ReadWord>();
			foreach (var response in Response.responses)
			{
				ProcessTextAnnotation(response.fullTextAnnotation, ret);
			}
			return ret;
		}


		// non public ---------
		string apiKey;
		UnityWebRequest webRequest;
		Color32[] prevTexels;
		int prevWidth;

		static bool FindDiff(Color32[] texels0, int width0, Color32[] texels1, int width1, RectInt rect)
		{
			var ret = false;
			for (var y = rect.y; y < (rect.y + rect.height); y++)
			{
				for (var x = rect.x; x < (rect.x + rect.width); x++)
				{
					var c0 = texels0[(y * width0) + x];
					var c1 = texels1[(y * width1) + x];
					if ((c0.r != c1.r) || (c0.g != c1.g) || (c0.b != c1.b))
					{
						ret = true;
						break;
					}
				}
			}
			return ret;
		}
	
		static void ProcessTextAnnotation(TextAnnotation textAnnotation, List<ReadWord> wordsOut)
		{
			if (textAnnotation.pages != null)
			{
				foreach (var page in textAnnotation.pages)
				{
					ProcessPage(page, wordsOut);
				}
			}
		}

		static void ProcessPage(Page page, List<ReadWord> wordsOut)
		{
			foreach (var block in page.blocks)
			{
				ProcessBlock(block, wordsOut);
			}
		}

		static void ProcessBlock(Block block, List<ReadWord> wordsOut)
		{
			foreach (var paragraph in block.paragraphs)
			{
				ProcessParagraphs(paragraph, wordsOut);
			}
		}

		static void ProcessParagraphs(Paragraph paragraph, List<ReadWord> wordsOut)
		{
			foreach (var word in paragraph.words)
			{
				var readWord = ProcessWord(word);
				if (readWord != null)
				{
					wordsOut.Add(readWord);
				}
			}
		}

		static ReadWord ProcessWord(Word word)
		{
			var ret = new ReadWord();
			ret.letters = new List<ReadLetter>();
			foreach (var symbol in word.symbols)
			{
				var letter = ProcessSymbol(symbol);
				ret.letters.Add(letter);
				ret.text += letter.text;
			}
			return ret;
		}

		static ReadLetter ProcessSymbol(Symbol symbol)
		{
			var ret = new ReadLetter();
			ret.vertices = new List<Vector2>();
			// 頂点抽出
			var srcVertices = symbol.boundingBox.vertices;
			for (var i = 0; i < srcVertices.Count; i++)
			{
				var srcV = srcVertices[i];
				ret.vertices.Add(new Vector2(srcV.x, srcV.y));
			}

			ret.text = symbol.text;
			return ret;
		}

		// request Data Types.
		[Serializable]
		class RequestBody
		{
			public List<AnnotateImageRequest> requests;
		}

		[Serializable]
		class AnnotateImageRequest
		{
			public Image image;
			public List<Feature> features;
			//public string imageContext;
		}

		[Serializable]
		class Image
		{
			public string content;
			//public ImageSource source;
		}

		[Serializable]
		class ImageSource
		{
			public string gcsImageUri;
		}

		[Serializable]
		class Feature
		{
			public string type;
			public int maxResults;
		}

		enum FeatureType 
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
		class ImageContext
		{
			public LatLongRect latLongRect;
			public string languageHints;
		}

		[Serializable]
		class LatLongRect
		{
			public LatLng minLatLng;
			public LatLng maxLatLng;
		}
	}
}