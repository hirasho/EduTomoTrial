using UnityEngine;
using UnityEditor;

public class ScreenShotCapturer
{
	[MenuItem("Hirasho/Screenshot %F3", false)]
	public static void Capture()
	{
		var path = "Recordings/ScreenShot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
		ScreenCapture.CaptureScreenshot(path);
	}
}
