using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
			GUILayout.TextField(prj[i]);
			if (GUILayout.Button("..."))
				prj[i] = EditorUtility.OpenFilePanelWithFilters("C# Project", "../", new string[] { "Project", "proj" });
			if (GUILayout.Button("Remove"))
			{
				if (guid == null || guid.Length != prj.Length)
				{
					guid = new string[prj.Length];
					for (int n = 0; n < c; n++)
						guid[n] = Guid.NewGuid().ToString().ToUpper();
				}

				ArrayUtility.RemoveAt(ref prj, i);
				ArrayUtility.RemoveAt(ref guid, i);

				settings.additionalProjects = prj;
				settings.additionalProjectGUID = guid;
				CsSolutionSettings.Save();
				GUIUtility.ExitGUI();
			}

			GUILayout.EndHorizontal();
		}

		if (GUILayout.Button("Add"))
		{
			var add = EditorUtility.OpenFilePanelWithFilters("C# Project", "../", new string[] { "Project", "csproj" });

			if (!string.IsNullOrEmpty(add))
			{
				if (guid == null || guid.Length != prj.Length)
				{
					guid = new string[prj.Length];
					for (int n = 0, c = prj.Length; n < c; n++)
						guid[n] = Guid.NewGuid().ToString().ToUpper();
				}

				ArrayUtility.Add(ref prj, add);
				ArrayUtility.Add(ref guid, Guid.NewGuid().ToString().ToUpper());
				settings.additionalProjects = prj;
				settings.additionalProjectGUID = guid;
				CsSolutionSettings.Save();
			}
		}
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
