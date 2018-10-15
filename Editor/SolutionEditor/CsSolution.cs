using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Unity.Karl.Editor
{
	sealed class CsSolution
	{
		string m_Path;

		public CsSolution(string path)
		{
			m_Path = path;
		}

		public IEnumerable<ProjectAndGuid> GetProjects()
		{
			var projects = new List<ProjectAndGuid>();

			string solutionContents = File.ReadAllText(m_Path);

			using (var sr = new StringReader(solutionContents))
			{
				while (sr.Peek() > -1)
				{
					var line = sr.ReadLine();

					if (line.StartsWith("Project(\""))
					{
						var separator = line.IndexOf("=");
						var project = line.Substring(separator, line.Length - separator);
						projects.Add(new ProjectAndGuid()
						{
							path = project
						});
					}
				}
			}

			return projects;
		}

		public void AddProjectReferences(IEnumerable<ProjectAndGuid> projects)
		{
			var slnText = File.ReadAllText(m_Path);
			var add = projects.Where(x => !slnText.Contains(x.guid));

			if (!add.Any())
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
					foreach (var prj in add)
					{
						var proj = prj.path;
						var name = Path.GetFileNameWithoutExtension(proj);
						var guid = prj.guid;

						sb.AppendLine(string.Format("Project(\"{{{0}}}\") = \"{1}\", \"{2}\", \"{{{3}}}\"", slnGuid, name, proj, guid));
						sb.AppendLine("EndProject");
					}
				}

				sb.AppendLine(line);

				if (line.Contains("GlobalSection(ProjectConfigurationPlatforms)"))
				{
					foreach (var prj in add)
					{
						var guid = prj.guid;

						sb.AppendLine(string.Format("\t\t{{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", guid));
						sb.AppendLine(string.Format("\t\t{{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU", guid));
						sb.AppendLine(string.Format("\t\t{{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU", guid));
						sb.AppendLine(string.Format("\t\t{{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU", guid));
					}
				}
			}

			sr.Dispose();

			File.WriteAllText(m_Path, sb.ToString());
		}
	}
}
