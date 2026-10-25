using System;
using System.IO;
using HybridCLR;
using HybridCLR.Commands;
using HybridCLR.Installer;
using HybridCLR.Settings;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ProbeBuild {
 public static void PrepareAndBuild() {
  var target=BuildTarget.StandaloneWindows64;
  if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,target)) throw new Exception("Switch target failed");
  PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.IL2CPP);
  PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone,Il2CppCompilerConfiguration.Debug);
  EditorUserBuildSettings.development=true;
  EditorUserBuildSettings.allowDebugging=false;
  UnityEditor.WindowsStandalone.UserBuildSettings.createSolution=false;
  PlayerSettings.productName="HybridCLRReadingProbe";
  PlayerSettings.companyName="TwoEggExperiment";
  var settings=HybridCLRSettings.instance;
  settings.enable=true; settings.useGlobalIl2cpp=false;
  settings.hotUpdateAssemblies=new[]{"HotUpdate"};
  HybridCLRSettings.Save();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  EditorSceneManager.SaveScene(scene,"Assets/Probe.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Probe.unity",true)};
  var root=Directory.GetParent(Application.dataPath).Parent.FullName;
  Debug.Log("PROBE_INSTALL_START "+Application.unityVersion);
  if (!new InstallerController().HasInstalledHybridCLR()) new InstallerController().InstallFromLocal(Path.Combine(root,"combined-libil2cpp"));
  Debug.Log("PROBE_GENERATE_START runtime="+SettingsUtil.LocalIl2CppDir);
  PrebuildCommand.GenerateAll();
  File.Copy(Path.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target),"HotUpdate.dll"),"Assets/StreamingAssets/HotUpdate.dll.bytes",true);
  AssetDatabase.Refresh();
  Debug.Log("PROBE_BUILD_START");
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Probe.unity"}, locationPathName=Path.Combine(root,"Player/HybridCLRReadingProbe.exe"),target=target,options=BuildOptions.Development});
  Debug.Log("PROBE_BUILD_RESULT "+report.summary.result);
  if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Player build failed");
 }
}
