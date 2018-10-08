using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Karl.Editor
{
	[CustomEditor(typeof(CsProjectSettings))]
	sealed class CsProjectSettingsEditor : UnityEditor.Editor
	{
		ReorderableList m_AdditionalProjectReferencesList;
		ReorderableList m_RemoveReferencesList;

		SerializedProperty m_AdditionalProjectReferences;
		SerializedProperty m_RemoveReferences;

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

		void OnEnable()
		{
			m_AdditionalProjectReferences = serializedObject.FindProperty("m_AdditionalProjectReferences");
			m_AdditionalProjectReferencesList = new ReorderableList(serializedObject, m_AdditionalProjectReferences);
			m_AdditionalProjectReferencesList.drawHeaderCallback += (r) => { OnDrawHeader(r, "Additional Project References"); };
			m_AdditionalProjectReferencesList.drawElementCallback += DrawAdditionalProjectReferenceItem;

			m_RemoveReferences = serializedObject.FindProperty("m_RemoveReferences");
			m_RemoveReferencesList = new ReorderableList(serializedObject, m_RemoveReferences);
			m_RemoveReferencesList.drawHeaderCallback += (r) => { OnDrawHeader(r, "Include Reference Filter Patterns"); };
			m_RemoveReferencesList.drawElementCallback += DrawRemoveReferenceItem;
		}

		public override void OnInspectorGUI()
		{
			Styles.Init();

			serializedObject.Update();

			GUILayout.BeginVertical(Styles.listWrapper);

			GUILayout.Label("Additional C# Project References", EditorStyles.boldLabel);

			m_AdditionalProjectReferencesList.DoLayoutList();

			GUILayout.Space(4);

			GUILayout.Label("Include Reference Filter Patterns", EditorStyles.boldLabel);

			GUILayout.Label("Exclude references matching a regular expression.");

			m_RemoveReferencesList.DoLayoutList();

			GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}

		void OnDrawHeader(Rect rect, string title)
		{
			GUI.Label(rect, title);
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
				new GUIContent("Pattern"));
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
