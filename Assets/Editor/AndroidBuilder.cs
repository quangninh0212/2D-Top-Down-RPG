using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the APK. Split out from the setup wizard so a build can be re-run
// without regenerating content.
public static class AndroidBuilder
{
    private const string OutputPath = "Builds/Android/SoulboundGate.apk";

    [MenuItem("Tools/Soulbound Gate/Build Android APK")]
    public static void BuildApk()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            SoulboundSetupLog.Warn("Android Build Support is not installed; see MANUAL_STEPS.md.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) { scenes.Add(scene.path); }
        }

        if (scenes.Count == 0)
        {
            SoulboundSetupLog.Warn("No scenes enabled in Build Settings; nothing to build.");
            return;
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            SoulboundSetupLog.Step(string.Format("APK built: {0} ({1:0.0} MB, {2})",
                OutputPath, summary.totalSize / (1024f * 1024f), summary.totalTime));
        }
        else
        {
            SoulboundSetupLog.Warn("APK build " + summary.result + " with " + summary.totalErrors + " errors.");
        }
    }
}
