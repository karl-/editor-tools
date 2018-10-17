using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Karl.Editor
{
	[CustomEditor(typeof(CsProjectSettings))]
	sealed class CsProjectSettingsEditor : UnityEditor.Editor
	{
	    bool m_PropertiesInitialized;
		ReorderableList m_AdditionalProjectReferencesList;
		ReorderableList m_RemoveReferencesList;

		SerializedProperty m_AdditionalProjectReferences;
		SerializedProperty m_ReferenceFilters;
		SerializedProperty m_AdditionalProjectReferencesOverridesDlls;

//		List<ProjectAndGuid> m_Projects = new List<ProjectAndGuid>();

		static class Styles
		{
			static bool s_Initialized;

			public static GUIStyle listWrapper;

			public static void Init()
			{
				if (s_Initialized)
					return;
				s_Initialized = true;

				listWrapper = new GUIStyle()
				{
					margin = new RectOffset(4, 4, 4, 4)
				};
			}
		}

		void InitProperties()
		{
		    if (m_PropertiesInitialized || serializedObject == null)
		        return;

		    m_PropertiesInitialized = true;

			m_AdditionalProjectReferences = serializedObject.FindProperty("m_AdditionalProjectReferences");
			m_AdditionalProjectReferencesList = new ReorderableList(serializedObject, m_AdditionalProjectReferences);
			m_AdditionalProjectReferencesList.drawElementCallback += DrawAdditionalProjectReferenceItem;
			m_AdditionalProjectReferencesList.drawHeaderCallback += (r) =>
			{
				OnDrawHeader(r, new GUIContent("Additional Project References", "Include references to additional csproj files."));
			};


			m_ReferenceFilters = serializedObject.FindProperty("m_ProjectFilters");
			m_RemoveReferencesList = new ReorderableList(serializedObject, m_ReferenceFilters);
			m_RemoveReferencesList.drawElementCallback += DrawRemoveReferenceItem;
			m_RemoveReferencesList.drawHeaderCallback += (r) =>
				{ OnDrawHeader(r, new GUIContent("Include Reference Filter Patterns", "Remove referenced DLLs matching a regular expression.")); };

//			m_Projects = new CsSolution(CsProjectSettings.solutionPath).GetProjects().ToList();

			m_AdditionalProjectReferencesOverridesDlls = serializedObject.FindProperty("m_AdditionalProjectReferencesOverridesDlls");
		}

		public override void OnInspectorGUI()
		{
			Styles.Init();
		    InitProperties();

			var evt = Event.current;

			if (evt.type == EventType.ContextClick)
				DoContextMenu();

			serializedObject.Update();

			GUILayout.BeginVertical(Styles.listWrapper);

			GUILayout.Label("Solution Settings", EditorStyles.boldLabel);

			m_AdditionalProjectReferencesList.DoLayoutList();

			EditorGUILayout.PropertyField(m_AdditionalProjectReferencesOverridesDlls);

			GUILayout.Space(4);

			GUILayout.Label("General Project Settings", EditorStyles.boldLabel);

			m_RemoveReferencesList.DoLayoutList();

			GUILayout.EndVertical();

//			foreach (var prj in m_Projects)
//				GUILayout.Label(prj.path, EditorStyles.wordWrappedLabel);

			serializedObject.ApplyModifiedProperties();
		}

		void OnDrawHeader(Rect rect, GUIContent content)
		{
			GUI.Label(rect, content, EditorStyles.boldLabel);
		}

		void DrawAdditionalProjectReferenceItem(Rect rect, int index, bool isactive, bool isfocused)
		{
			var element = m_AdditionalProjectReferencesList.serializedProperty.GetArrayElementAtIndex(index);
			var projectPath = element.FindPropertyRelative("m_Path");
			EditorGUI.PropertyField(new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight), projectPath);
		}

		void DrawRemoveReferenceItem(Rect rect, int index, bool isactive, bool isfocused)
		{
			var element = m_RemoveReferencesList.serializedProperty.GetArrayElementAtIndex(index);

			EditorGUI.PropertyField(
				new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight),
				element,
				new GUIContent("Pattern", "Exclude references matching a regular expression."));
		}

		void DoContextMenu()
		{
		}
	}

	static class CsProjectSettingsProvider
	{
		static Vector2 s_Scroll;
		static UnityEditor.Editor m_SettingsEditor;

		[PreferenceItem("C# Solution")]
		static void CsSolutionSettingsPrefs()
		{
			var settings = CsProjectSettings.instance;

			UnityEditor.Editor.CreateCachedEditor(settings, typeof(CsProjectSettingsEditor), ref m_SettingsEditor);

			EditorGUI.BeginChangeCheck();

			m_SettingsEditor.OnInspectorGUI();

			if (EditorGUI.EndChangeCheck())
				settings.Save();


			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Rebuild CS Projects"))
				CsProjectSettings.RebuildSolution();
		}
	}
}
