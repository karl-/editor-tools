using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UEditor = UnityEditor.Editor;

namespace Unity.Karl.Editor
{
	[Serializable]
	class ProjectAndGuid
	{
		[SerializeField]
		string m_Path;

		[SerializeField]
		string m_Guid;

		public string path
		{
			get { return m_Path; }
			set { m_Path = value; }
		}

		public string guid
		{
			get
			{
				if (string.IsNullOrEmpty(m_Guid))
					m_Guid = Guid.NewGuid().ToString().ToUpper();
				return m_Guid;
			}
		}
	}

	sealed class CsProjectSettings : ScriptableObject
	{
		const string k_SettingsPath = "ProjectSettings/CSharpSolutionSettings.json";
		static CsProjectSettings s_Instance;

		[SerializeField]
		ProjectAndGuid[] m_AdditionalProjectReferences;

		[SerializeField]
		string[] m_RemoveReferences;

		public IEnumerable<ProjectAndGuid> additionalProjectReferences
		{
			get { return new ReadOnlyCollection<ProjectAndGuid>(m_AdditionalProjectReferences); }
		}

		public IEnumerable<string> removeReferencePatterns
		{
			get { return new ReadOnlyCollection<string>(m_RemoveReferences); }
		}

		public static CsProjectSettings instance
		{
			get
			{
				if (s_Instance == null)
				{
					if (File.Exists(k_SettingsPath))
					{
						try
						{
							s_Instance = ScriptableObject.CreateInstance<CsProjectSettings>();
							JsonUtility.FromJsonOverwrite(File.ReadAllText(k_SettingsPath), s_Instance);
						}
						catch (Exception e)
						{
							Debug.LogError(e);
						}
					}

					if (s_Instance == null)
						s_Instance = ScriptableObject.CreateInstance<CsProjectSettings>();
				}

				return s_Instance;
			}
		}

		public void Save()
		{
			File.WriteAllText(k_SettingsPath, JsonUtility.ToJson(this, true));
		}

		public static void RebuildSolution()
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
}
