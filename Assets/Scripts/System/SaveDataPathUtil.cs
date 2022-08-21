using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SaveDataPathUtil
{
    // relativePathは例えばcache/hoge.json。これがpersistentDataPathか何かに追加されて返される
    public static string MakeFullPath(string relativePath)
    {
        var dir = GetFolderPath();
        return Path.Combine(dir, relativePath);
    }

    public static string GetFolderPath()
    {
        var dir = Application.persistentDataPath;
#if UNITY_EDITOR || UNITY_STANDALONE // エディタとPCビルドでは見やすい場所にファイルを置く。
        dir = Path.GetFullPath(".");
        dir = Path.Combine(dir, "persistentData");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
#endif
        return dir;
    }
#if UNITY_EDITOR
    [MenuItem("Hirasho/OpenPersistentData")]
    public static void OpenPersistentData()
    {
        var path = GetFolderPath();
        EditorUtility.RevealInFinder(path);
    }
#endif
}
