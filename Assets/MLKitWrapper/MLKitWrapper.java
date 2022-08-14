package com.hirasho.MLKitWrapper;

import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.text.Text;
import com.google.mlkit.vision.text.TextRecognition;
import com.google.mlkit.vision.text.TextRecognizer;
import com.google.mlkit.vision.text.latin.TextRecognizerOptions;
import com.unity3d.player.UnityPlayer;
import com.google.gson.Gson;

public class MLKitWrapper 
{
	static public class Input
	{
		int width;
		int height;
		int[] pixels;
	}
	
	static public class Output
	{
		String message;
	}
	
	static public void RecognizeText(String receiverGameObjectName, String inputJson) 
	{
		TextRecognizer recognizer = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS);

		Gson gson = new Gson();
		Input input = gson.fromJson(inputJson, Input.class);

		Output output = new Output();
		output.message = "Hey!";

		String outputJson = gson.toJson(output);

		// TODO:いろいろあって完了後に
		UnityPlayer.UnitySendMessage(receiverGameObjectName, "OnComplete", outputJson);
	}

	static public int IncrementTest(int a) 
	{
		return a + 1;
	}
}

