﻿using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;

class CsSolutionSettings
{
	const string k_SettingsPath = "ProjectSettings/CSharpSolutionSettings.json";
	static CsSolutionSettings s_Instance;
	public string[] additionalProjects = new string[0];
	public string[] additionalProjectGUID = new string[0];

	public static CsSolutionSettings instance
	{
		get
		{
			if (s_Instance == null)
			{
				if (File.Exists(k_SettingsPath))
				{
					try
					{
						s_Instance = JsonUtility.FromJson<CsSolutionSettings>(File.ReadAllText(k_SettingsPath));
					}
					catch(Exception e)
					{
						Debug.LogError(e);
					}
				}

				if(s_Instance == null)
					s_Instance = new CsSolutionSettings();
			}

			return s_Instance;
		}
	}

	public static void Save()
	{
		File.WriteAllText(k_SettingsPath, JsonUtility.ToJson(instance));
	}
}

static class CsSolutionSettingsEditor
{
	[PreferenceItem("C# Solution")]
	static void CsSolutionSettingsPrefs()
	{
		var settings = CsSolutionSettings.instance;

		GUILayout.Label("Additional C# Projects", EditorStyles.boldLabel);

		var prj = settings.additionalProjects;
		var guid = settings.additionalProjectGUID;

		for (int i = 0, c = prj.Length; i < c; i++)
		{
			GUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();

			prj[i] = GUILayout.TextField(prj[i]);

			if (EditorGUI.EndChangeCheck())
				Save(settings);

			if (GUILayout.Button("...", GUILayout.Width(32)))
				prj[i] = EditorUtility.OpenFilePanelWithFilters("C# Project", "../", new string[] { "Project", "csproj" });

			if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
			{
				if (guid == null || guid.Length != prj.Length)
				{
					guid = new string[prj.Length];
					for (int n = 0; n < c; n++)
						guid[n] = Guid.NewGuid().ToString().ToUpper();
				}

				ArrayUtility.RemoveAt(ref prj, i);
				ArrayUtility.RemoveAt(ref guid, i);
				Save(settings);
			}

			GUILayout.EndHorizontal();
		}

		if (GUILayout.Button("Add"))
		{
			ArrayUtility.Add(ref prj, "");
			ArrayUtility.Add(ref guid, Guid.NewGuid().ToString().ToUpper());
			CsSolutionSettings.Save();
		}

	}

	static void Save(CsSolutionSettings settings)
	{
		CsSolutionSettings.Save();
		GUIUtility.ExitGUI();
	}

	static void RebuildCsProject()
	{
		Type type = typeof(Editor).Assembly.GetType("UnityEditor.VisualStudioIntegration.SolutionSynchronizer");
		ConstructorInfo ctor = type.GetConstructor(new[] { typeof(string) });
		object sync = ctor.Invoke(new object[] { Directory.GetParent(Application.dataPath).FullName });

		if (sync == null)
			return;

		Debug.Log("woot");

/*
		var scriptEditor = ScriptEditorUtility.GetScriptEditorFromPreferences();
//		sync.GenerateAndWriteSolutionAndProjects(scriptEditor);
		AssetPostprocessingInternal.CallOnGeneratedCSProjectFiles();
*/

	}
}

class CsProjectPostProcessor : AssetPostprocessor
{
	static void OnGeneratedCSProjectFiles()
	{
		foreach (var sln in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln"))
			AppendProjects(sln);
	}

	static void AppendProjects(string sln)
	{
		var settings = CsSolutionSettings.instance;

		var slnText = File.ReadAllText(sln);
		var addProjects = new Dictionary<string, string>();

		for(int i = 0, c = settings.additionalProjects.Length; i < c; i++)
		{
			var prj = settings.additionalProjects[i];
			var id = settings.additionalProjectGUID[i];

			if (File.Exists(prj) && !slnText.Contains(prj))
				addProjects.Add(prj, id);
		}

		if (!addProjects.Any())
			return;

		var sr = new StringReader(slnText);
		var sb = new StringBuilder();
		string slnGuid = "";

		while (sr.Peek() > -1)
		{
			var line = sr.ReadLine();

			if(string.IsNullOrEmpty(slnGuid) && line.StartsWith("Project(\""))
				slnGuid = line.Substring(10, 36);

			// end of projects
			if (line.Equals("Global"))
			{
				foreach(var kvp in addProjects)
				{
					var proj = kvp.Key;
					var name = Path.GetFileNameWithoutExtension(proj);
					var guid = kvp.Value;

					sb.AppendLine(string.Format("Project(\"{{{0}}}\") = \"{1}\", \"{2}\", \"{{{3}}}\"", slnGuid, name, proj, guid));
					sb.AppendLine("EndProject");
				}
			}

			sb.AppendLine(line);

			if (line.Contains("GlobalSection(ProjectConfigurationPlatforms)"))
			{
				foreach(var kvp in addProjects)
				{
					var guid = kvp.Value;

					sb.AppendLine(string.Format("\t\t{{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", guid));
					sb.AppendLine(string.Format("\t\t{{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU", guid));
					sb.AppendLine(string.Format("\t\t{{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU", guid));
					sb.AppendLine(string.Format("\t\t{{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU", guid));
				}
			}
		}

		sr.Dispose();

		File.WriteAllText(sln, sb.ToString());
	}
}
