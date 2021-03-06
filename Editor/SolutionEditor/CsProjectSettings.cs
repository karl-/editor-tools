using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;
using UEditor = UnityEditor.Editor;

namespace Unity.Karl.Editor
{
	sealed class CsProjectSettings : ScriptableObject
	{
		const string k_SettingsPath = "ProjectSettings/CSharpSolutionSettings.json";
		static CsProjectSettings s_Instance;

		[SerializeField]
		CsProject[] m_AdditionalProjectReferences = new CsProject[]
		{
			new CsProject("C:/Users/karlh/unity/unity/Projects/CSharp/UnityEngine.csproj"),
			new CsProject("C:/Users/karlh/unity/unity/Projects/CSharp/UnityEditor.csproj")
		};

		[SerializeField]
		bool m_AdditionalProjectReferencesOverridesDlls;

		public bool additionalProjectReferencesOverridesDlls
		{
			get { return m_AdditionalProjectReferencesOverridesDlls; }
		}

		// Used to remove the module DLLs that are made redundant by including the UnityEngine/UnityEditor projects.
		// Ex, to filter out modules while leaving two that are still required:
		// @"(?!UnityEngine\.TestRunner|UnityEngine\.AudioModule)UnityEngine\.?"
		[FormerlySerializedAs("m_RemoveReferences")]
		[SerializeField]
		string[] m_ReferenceFilters = new string[] { @"(?!UnityEngine\.TestRunner|UnityEngine\.AudioModule)UnityEngine\.?" };

		[SerializeField]
		string[] m_ProjectFilters;

		public IEnumerable<CsProject> additionalProjectReferences
		{
			get { return new ReadOnlyCollection<CsProject>(m_AdditionalProjectReferences); }
			set { m_AdditionalProjectReferences = value.ToArray(); }
		}

		public IEnumerable<string> referenceFilters
		{
			get { return new ReadOnlyCollection<string>(m_ReferenceFilters); }
			set { m_ReferenceFilters = value.ToArray(); }
		}

		public IEnumerable<string> projectFilters
		{
			get { return new ReadOnlyCollection<string>(m_ProjectFilters); }
			set { m_ProjectFilters = value.ToArray(); }
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

		public static string solutionPath
		{
			get
			{
				return Directory.GetCurrentDirectory() + "/" + Application.productName + ".sln";
			}
		}

		public static void RebuildSolution()
		{
			foreach(var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj"))
				File.Delete(file);

			foreach(var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln"))
				File.Delete(file);

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
