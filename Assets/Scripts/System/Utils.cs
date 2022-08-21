using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
	//要素数が n の配列 a をシャッフルする(添字は0からn-1):
	public static void Shuffle<T>(IList<T> a)
	{
		//  i を n - 1 から 1 まで減少させながら、以下を実行する
		for (var i = (a.Count - 1); i >= 1; i--)
		{
			// j に 0 以上 i 以下のランダムな整数を代入する
			var j = Random.Range(0, i + 1);
			// a[j] と a[i]を交換する
			var tmp = a[i];
			a[i] = a[j];
			a[j] = tmp;
		}
	}
}
