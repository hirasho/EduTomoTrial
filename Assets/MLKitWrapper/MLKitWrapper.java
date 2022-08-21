package com.hirasho.MLKitWrapper;

import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.gms.tasks.Task;
import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.text.Text;
import com.google.mlkit.vision.text.TextRecognition;
import com.google.mlkit.vision.text.TextRecognizer;
import com.google.mlkit.vision.text.latin.TextRecognizerOptions;
import com.unity3d.player.UnityPlayer;
import com.google.gson.Gson;
import android.graphics.Bitmap;
import android.graphics.Rect;
import android.graphics.Point;
import java.util.List;
import android.util.Log;

public class MLKitWrapper 
{
	static public class Input
	{
		static public Input deserialize(String serialized)
		{
			Input ret = new Input();
			ret.requestId = readInt(serialized, 0);
			ret.width = readInt(serialized, 2);
			ret.height = readInt(serialized, 4);
			int n = ret.width * ret.height;
			ret.pixels = new int[n];
			for (int i = 0; i < n; i++)
			{
				ret.pixels[i] = readInt(serialized, 6 + (i * 2));
			}
			return ret;
		}

		public int requestId;
		public int width;
		public int height;
		public int[] pixels;

		static int readInt(String serialized, int pos)
		{
			int ret = serialized.charAt(pos);
			ret |= serialized.charAt(pos + 1) << 16;
			return ret;
		}
	}
	
	static public class Output
	{
		public int requestId;
		public TextData text;
		public String errorMessage;
	}

	// 以下~Dataで終わるクラスはMLKitのクラスを写したもの
	static class TextData
	{
		public String text;
		public TextBlockData[] textBlocks;
	}

	static class TextBlockData
	{
		public RectData boundingBox;
		public PointData[] cornerPoints;
		public LineData[] lines;
		public String recognizedLanguage;
		public String text;
	}

	static class LineData
	{
		public RectData boundingBox;
		public PointData[] cornerPoints;
		public ElementData[] elements;
		public String recognizedLanguage;
		public String text;
	}

	static class ElementData
	{
		public RectData boundingBox;
		public PointData[] cornerPoints;
		public String recognizedLanguage;
		public String text;
	}

	static class RectData
	{
		public String toString()
		{
			return "(" + left + "," + top + ") - (" + right + "," + bottom + ")";
		}
		public int bottom;
		public int left;
		public int right;
		public int top;
	}

	static class PointData
	{
		public int x;
		public int y;
	}
	
	static public void recognizeText(String receiverGameObjectName, String serializedInput) 
	{
		TextRecognizer recognizer = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS);

//		Gson gson = new Gson();
//		Input input = gson.fromJson(serializedInput, Input.class);
		Input input = Input.deserialize(serializedInput);
Log.d("MLKit", "input " + Integer.toString(input.width) + "x" + Integer.toString(input.height) + " pixelCount=" + Integer.toString(input.pixels.length) + " id=" + Integer.toString(input.requestId));

		Bitmap bitmap = Bitmap.createBitmap(input.pixels, input.width, input.height, Bitmap.Config.ARGB_8888);
		if (bitmap == null)
		{
			Log.e("MLKit", "Bitmap.createBitmap failed.");
		}
		else
		{
			InputImage inputImage = InputImage.fromBitmap(bitmap, 0);
			if (inputImage == null)
			{
				Log.e("MLKit", "InputImage.fromBitmap failed.");
			}
			else
			{
				Output output = new Output();
				output.requestId = input.requestId;

				OnSuccess onSuccess = new OnSuccess(receiverGameObjectName, output);
				OnFailure onFailure = new OnFailure(receiverGameObjectName, output);
				Task<Text> result = recognizer.process(inputImage).
					addOnSuccessListener(onSuccess).
					addOnFailureListener(onFailure);
			}
		}
	}

	static class OnSuccess implements OnSuccessListener<Text>
	{
		public OnSuccess(String receiverGameObjectName, Output output)
		{
			this.receiverGameObjectName = receiverGameObjectName;
			this.output = output;
		}

		@Override public void onSuccess(Text text) 
		{
			if (text == null)
			{
				Log.e("MLKit", "OnSuccess.onSuccess text is null.");
			}
			else
			{
				TextData textData = new TextData();
				try
				{
					output.text = parseText(text);
				}
				catch (Exception e)
				{
					Log.e("MLKit", "OnSuccess parseText throw exception: " + e.getMessage());
				}

				if (output.text != null)
				{
					Gson gson = new Gson();
					String outputJson = gson.toJson(output);
					UnityPlayer.UnitySendMessage(receiverGameObjectName, "OnComplete", outputJson);
				}
			}
		}
		// non public ----
		String receiverGameObjectName;
		Output output;

		TextData parseText(Text src)
		{
			TextData ret = new TextData();
			ret.text = src.getText();

			List<Text.TextBlock> srcTextBlocks = src.getTextBlocks();
			if (srcTextBlocks != null)
			{
				ret.textBlocks = new TextBlockData[srcTextBlocks.size()];
Log.d("MLKit", "parseText " + ret.text + " " + ret.textBlocks.length);
				for (int i = 0; i < srcTextBlocks.size(); i++)
				{
					TextBlockData block = parseTextBlock(srcTextBlocks.get(i));
					ret.textBlocks[i] = block;
				}
			}
			else
			{
				Log.w("MLKit", "OnSuccess.parseText getTextBlocks returned null.");
			}
			return ret;
		}

		TextBlockData parseTextBlock(Text.TextBlock src)
		{
			TextBlockData ret = new TextBlockData();
			ret.boundingBox = parseRect(src.getBoundingBox());
			ret.cornerPoints = parsePoints(src.getCornerPoints());
			ret.recognizedLanguage = src.getRecognizedLanguage();
			ret.text = src.getText();

			List<Text.Line> srcLines = src.getLines();
			if (srcLines != null)
			{
				ret.lines = new LineData[srcLines.size()];
Log.d("MLKit", "parseTextBlock " + ret.text + " " + ret.lines.length + " " + ret.boundingBox);
				for (int i = 0; i < srcLines.size(); i++)
				{
					LineData line = parseLine(srcLines.get(i));
					ret.lines[i] = line;
				}
			}
			else
			{
				Log.w("MLKit", "OnSuccess.parseTextBlock getLines returned null.");
			}
			return ret;
		}

		LineData parseLine(Text.Line src)
		{
			LineData ret = new LineData();
			ret.boundingBox = parseRect(src.getBoundingBox());
			ret.cornerPoints = parsePoints(src.getCornerPoints());
			ret.recognizedLanguage = src.getRecognizedLanguage();
			ret.text = src.getText();

			List<Text.Element> srcElements = src.getElements();
			if (srcElements != null)
			{
				ret.elements = new ElementData[srcElements.size()];
Log.d("MLKit", "parseLine " + ret.text + " " + ret.elements.length + " " + ret.boundingBox);
				for (int i = 0; i < srcElements.size(); i++)
				{
					ElementData element = parseElement(srcElements.get(i));
					ret.elements[i] = element;
				}
			}
			else
			{
				Log.w("MLKit", "OnSuccess.parseLine getElements returned null.");
			}

			return ret;
		}

		ElementData parseElement(Text.Element src)
		{
			ElementData ret = new ElementData();
			ret.boundingBox = parseRect(src.getBoundingBox());
			ret.cornerPoints = parsePoints(src.getCornerPoints());
			ret.recognizedLanguage = src.getRecognizedLanguage();
			ret.text = src.getText();
Log.d("MLKit", "parseElement " + ret.text + " " + ret.boundingBox);
			return ret;
		}

		RectData parseRect(Rect src)
		{
			RectData ret = new RectData();
			ret.bottom = src.bottom;
			ret.left = src.left;
			ret.right = src.right;
			ret.top = src.top;
			return ret;
		}

		PointData parsePoint(Point src)
		{
			PointData ret = new PointData();
			ret.x = src.x;
			ret.y = src.y;
			return ret;
		}

		PointData[] parsePoints(Point[] src)
		{
			PointData[] ret = new PointData[src.length];
			for (int i = 0; i < src.length; i++)
			{
				ret[i] = parsePoint(src[i]);
			}
			return ret;
		}
	}

	static class OnFailure implements OnFailureListener
	{
		public OnFailure(String receiverGameObjectName, Output output)
		{
			this.receiverGameObjectName = receiverGameObjectName;
			this.output = output;
		}

		@Override public void onFailure(Exception e) 
		{
			output.errorMessage = e.getMessage();
Log.d("MLKit", "onFailure " + output.errorMessage);

			Gson gson = new Gson();
			String outputJson = gson.toJson(output);
			UnityPlayer.UnitySendMessage(receiverGameObjectName, "OnComplete", outputJson);
		}
		// non public ----
		String receiverGameObjectName;
		Output output;
	}

	static public int incrementTest(int a) 
	{
		return a + 1;
	}
}

