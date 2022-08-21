using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class CallStackBuildProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        // スタックトレースを抑制
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        // 手元でビルド後にdiffが出るのが面倒なので戻しておく
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
        AssetDatabase.SaveAssets();
    }
}