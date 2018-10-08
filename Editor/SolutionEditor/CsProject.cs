using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Unity.Karl.Editor
{
	class CsProject
	{
		string m_Path;

		public CsProject(string path)
		{
			m_Path = path;
		}

		static readonly HashSet<string> k_HintPathWhitelist = new HashSet<string>()
		{
			@"UnityEngine\.UnityTestProtocolModule",
			@"UnityEngine\.TestRunner",
			@"UnityEngine\.AudioModule"
		};

		public void RemoveReferences(IEnumerable<string> patterns)
		{
			string csproj = File.ReadAllText(m_Path);

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
					if (patterns.Any(x => Regex.IsMatch(name, x)))
					{
						ReadToLine(sr, "</Reference>");
						continue;
					}
				}

				sb.AppendLine(line);
			}

			File.WriteAllText(m_Path, sb.ToString());
			sr.Dispose();
		}

		public void AddProjectReferences(IEnumerable<ProjectAndGuid> projects)
		{
			string csproj = File.ReadAllText(m_Path);

			var sr = new StringReader(csproj);
			var sb = new StringBuilder();

			while (sr.Peek() > -1)
			{
				var line = sr.ReadLine();
				var trim = line.Trim();

				if (trim.StartsWith("</Project>"))
				{
					sb.AppendLine("  <ItemGroup>");

					foreach(var prj in projects)
					{
						sb.AppendLine("    <ProjectReference Include=\"" + prj.path + "\">");
						sb.AppendLine("      <Project>{" + prj.guid + "}</Project>");
						sb.AppendLine("      <Name>" + Path.GetFileNameWithoutExtension(prj.path) + "</Name>");
						sb.AppendLine("    </ProjectReference>");

					}
					sb.AppendLine("  </ItemGroup>");
				}

				sb.AppendLine(line);
			}

			File.WriteAllText(m_Path, sb.ToString());
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
	}
}
