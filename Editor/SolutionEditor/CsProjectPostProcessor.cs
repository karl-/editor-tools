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
			var instance = CsProjectSettings.instance;

			foreach (var solutionPath in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln"))
			{
				var solution = new CsSolution(solutionPath);
				solution.AddProjectReferences(instance.additionalProjectReferences);
			}

			foreach (var projectPath in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj"))
			{
				var prj = new CsProject(projectPath);

				if(instance.additionalProjectReferencesOverridesDlls)
				{
					prj.AddProjectReferences(instance.additionalProjectReferences);
					prj.RemoveReferences(instance.referenceFilters);
				}
			}
		}

		static IEnumerable<string> GetFiles(string path)
		{
			var all = SearchOption.TopDirectoryOnly;

			if (path.EndsWith("!"))
			{
				path = path.Substring(0, path.Length - 1);
				all = SearchOption.AllDirectories;
			}

			var directory = "";
			var sanitized = path.Replace("\\", "/");
			var separator = sanitized.LastIndexOf("/");
			var pattern = sanitized;

			if (separator > -1)
			{
				directory = sanitized.Substring(0, separator + 1);
				pattern = sanitized.Substring(separator + 1, (sanitized.Length - separator) - 1);
			}

			return Directory.GetFiles(directory, pattern, all);
		}
	}
}
