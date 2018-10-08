using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UEditor = UnityEditor.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditorInternal;

namespace Unity.Karl.Editor
{
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
						catch (Exception e)
						{
							Debug.LogError(e);
						}
					}

					if (s_Instance == null)
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
		static Vector2 s_Scroll;

		[PreferenceItem("C# Solution")]
		static void CsSolutionSettingsPrefs()
		{
			var settings = CsSolutionSettings.instance;

			GUILayout.Label("Additional C# Projects", EditorStyles.boldLabel);

			s_Scroll = EditorGUILayout.BeginScrollView(s_Scroll);

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
					settings.additionalProjects = prj;
					settings.additionalProjectGUID = guid;
					Save(settings);
				}

				GUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add"))
			{
				ArrayUtility.Add(ref prj, "");
				ArrayUtility.Add(ref guid, Guid.NewGuid().ToString().ToUpper());
				settings.additionalProjects = prj;
				settings.additionalProjectGUID = guid;
				CsSolutionSettings.Save();
			}

			EditorGUILayout.EndScrollView();

			if (GUILayout.Button("Rebuild CS Projects"))
				RebuildCsProject();
		}

		static void Save(CsSolutionSettings settings)
		{
			CsSolutionSettings.Save();
			GUIUtility.ExitGUI();
		}

		static void RebuildCsProject()
		{
			Type type = typeof(UEditor).Assembly.GetType("UnityEditor.VisualStudioIntegration.SolutionSynchronizer");
			ConstructorInfo ctor = type.GetConstructor(new[] { typeof(string) });
			object sync = ctor.Invoke(new object[] { Directory.GetParent(Application.dataPath).FullName });
			if (sync == null)
				return;
			var rewriteSolutionMethod = type.GetMethod("GenerateAndWriteSolutionAndProjects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (rewriteSolutionMethod == null)
				return;
			var scriptEditor = ScriptEditorUtility.GetScriptEditorFromPreferences();
			rewriteSolutionMethod.Invoke(sync, new object[] { scriptEditor });
			CsProjectPostProcessor.OnGeneratedCSProjectFiles();
		}
	}

	class CsProjectPostProcessor : AssetPostprocessor
	{
		internal static void OnGeneratedCSProjectFiles()
		{
			foreach (var sln in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln"))
				AppendProjects(sln);

			foreach (var prj in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj"))
				ReplaceHintPathWithProject(prj);
		}

		static void AppendProjects(string sln)
		{
			var settings = CsSolutionSettings.instance;

			var slnText = File.ReadAllText(sln);
			var addProjects = new Dictionary<string, string>();

			for (int i = 0, c = settings.additionalProjects.Length; i < c; i++)
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

				if (string.IsNullOrEmpty(slnGuid) && line.StartsWith("Project(\""))
					slnGuid = line.Substring(10, 36);

				// end of projects
				if (line.Equals("Global"))
				{
					foreach (var kvp in addProjects)
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
					foreach (var kvp in addProjects)
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

		static readonly HashSet<string> k_HintPathWhitelist = new HashSet<string>()
		{
			@"UnityEngine\.UnityTestProtocolModule",
			@"UnityEngine\.TestRunner",
			@"UnityEngine\.AudioModule"
		};

		static void ReplaceHintPathWithProject(string projectPath)
		{
			string csproj = File.ReadAllText(projectPath);
			var references = new HashSet<string>(CsSolutionSettings.instance.additionalProjects.Select(Path.GetFileNameWithoutExtension));

			var sr = new StringReader(csproj);
			var sb = new StringBuilder();

			while (sr.Peek() > -1)
			{
				var line = sr.ReadLine();

				var trim = line.Trim();

				// Remove HintPath references
				if (trim.StartsWith("<Reference Include=\""))
				{
					var name = trim.Replace("<Reference Include=\"", "").Replace("\">", "");

					// If a match is found, advance the reader beyond this reference
					if (!k_HintPathWhitelist.Any(x => Regex.IsMatch(name, x))
						&& references.Any(x => Regex.IsMatch(name, x + "\\.?")))
					{
						ReadToLine(sr, "</Reference>");
						continue;
					}
				}

				// Add ProjectReference ItemGroup
				if (trim.StartsWith("</Project>"))
					AppendProjectReferenceItemGroup(sb,
						CsSolutionSettings.instance.additionalProjects,
						CsSolutionSettings.instance.additionalProjectGUID);

				sb.AppendLine(line);
			}

			File.WriteAllText(projectPath, sb.ToString());
			sr.Dispose();
		}

		static void ReadToLine(StringReader sr, string match)
		{
			while (sr.Peek() > -1)
			{
				var line = sr.ReadLine();
				var trim = line.Trim();
				if (trim.StartsWith(match))
					return;
			}
		}

		static void AppendProjectReferenceItemGroup(StringBuilder sb, string[] path, string[] guid)
		{
			sb.AppendLine("  <ItemGroup>");
			for (int i = 0, c = System.Math.Min(path.Length, guid.Length); i < c; i++)
			{
				sb.AppendLine("    <ProjectReference Include=\"" + path[i] + "\">");
				sb.AppendLine("      <Project>{" + guid[i] + "}</Project>");
				sb.AppendLine("      <Name>" + Path.GetFileNameWithoutExtension(path[i]) + "</Name>");
				sb.AppendLine("    </ProjectReference>");

			}
			sb.AppendLine("  </ItemGroup>");
		}
	}
}
