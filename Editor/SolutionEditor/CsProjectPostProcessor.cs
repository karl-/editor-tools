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
	class CsProjectPostProcessor : AssetPostprocessor
	{
		internal static void OnGeneratedCSProjectFiles()
		{
			foreach (var solutionPath in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln"))
			{
				var solution = new CsSolution(solutionPath);
				solution.AddProjectReferences(CsProjectSettings.instance.additionalProjectReferences);
			}

			foreach (var projectPath in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj"))
			{
				var prj = new CsProject(projectPath);
				prj.AddProjectReferences(CsProjectSettings.instance.additionalProjectReferences);
				prj.RemoveReferences(CsProjectSettings.instance.removeReferencePatterns);
			}
		}
	}
}
