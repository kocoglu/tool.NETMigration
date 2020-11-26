using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Tool
{
    public class Startup
    {
        public void Migrate()
        {
            Console.Write("Please enter solution directory path: ");
            var directory = Console.ReadLine();
            var solution = new Solution(directory);
            solution.Load();
            solution.Migrate();
        }
    }

    public class Solution
    {
        private readonly string Directory;
        private IEnumerable<Project> Projects;
        public Solution(string directory)
        {

            Directory = directory;
            if (directory == Constats.TestSolutionKey)
                Directory = Constats.TestSolutionDirectoryPath;
        }

        public void Load()
        {
            var csprojs = System.IO.Directory.GetFiles(Directory, "*.csproj", SearchOption.AllDirectories);
            Projects = csprojs.Select(s => new Project(Path.GetDirectoryName(s))).ToList();
        }

        public void Migrate()
        {
            foreach (var project in Projects)
            {
                project.Load();
                project.Migrate();
            }
        }
    }

    public class Project
    {
        private readonly string Directory;
        private FrameworkCsproj Csproj;
        private FrameworkPackage Package;

        public Project(string directory)
        {
            Directory = directory;
        }

        public void Load()
        {
            var csprojs = System.IO.Directory.GetFiles(Directory, "*.csproj", SearchOption.TopDirectoryOnly);
            if (!csprojs.Any()) throw new Exception("");
            if (csprojs.Count() > 1) throw new Exception("");

            var csprojFilePath = csprojs.FirstOrDefault();
            Csproj = new FrameworkCsproj(csprojFilePath);

            var packages = System.IO.Directory.GetFiles(Directory, "packages.config", SearchOption.TopDirectoryOnly);
            if (packages.Any())
            {
                var packagesFilePath = packages.FirstOrDefault();

                if (packages.Count() > 1) throw new Exception("");
                Package = new FrameworkPackage(packagesFilePath);
            }
        }

        public void Migrate()
        {
            MigrateCsproj();
            DeleteFrameworkItems();
        }

        private void MigrateCsproj()
        {
            Csproj.Load();
            if (Package != null)
                Package.Load();

            var netCsprojFilePath = Csproj.Path;
            var csprojByNet = new NetCsproj(netCsprojFilePath, Csproj.ProjectReferences, Package?.PackageReferences);
            csprojByNet.Build();
        }

        private void DeleteFrameworkItems()
        {
            var path = Path.Combine(Directory, "Properties");
            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, true);

            path = Path.Combine(Directory, "packages.config");
            if (File.Exists(path))
                File.Delete(path);
        }

    }

    public interface ICsproj
    {
        void Load();
    }

    public class MigrationFile
    {
        public string Path { get; private set; }

        public MigrationFile(string path)
        {
            Path = path;
        }
    }

    public class FrameworkCsproj : MigrationFile
    {
        public IEnumerable<string> ProjectReferences { get; private set; }

        public FrameworkCsproj(string path) : base(path)
        {

        }

        public void Load()
        {
            List<string> projects = new List<string>();

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(Path);

            XmlNamespaceManager manager = new XmlNamespaceManager(xmldoc.NameTable);
            manager.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

            foreach (XmlNode item in xmldoc.SelectNodes("//x:ProjectReference", manager))
            {
                var include = item.Attributes["Include"].Value;
                Console.WriteLine(string.Format("{0}", include));
                projects.Add(include);
            }

            ProjectReferences = projects.ToList();
        }
    }

    public class FrameworkPackage : MigrationFile
    {
        public IEnumerable<PackageReference> PackageReferences { get; private set; }

        public FrameworkPackage(string path) : base(path)
        {

        }

        public void Load()
        {
            List<PackageReference> packages = new List<PackageReference>();

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(Path);

            XmlNamespaceManager manager = new XmlNamespaceManager(xmldoc.NameTable);

            foreach (XmlNode item in xmldoc.SelectNodes("//package", manager))
            {
                var package = new Tool.PackageReference()
                {
                    Name = item.Attributes["id"].Value,
                    Version = item.Attributes["version"].Value
                };
                Console.WriteLine(string.Format("{0} {1}", package.Name, package.Version));
                packages.Add(package);
            }

            PackageReferences = packages.ToList();
        }
    }

    public class NetCsproj : MigrationFile
    {
        private IEnumerable<string> TargetFrameworks;
        private readonly IEnumerable<string> ProjectReferences;
        private readonly IEnumerable<PackageReference> PackageReferences;

        public NetCsproj(string path, IEnumerable<string> projectReferences, IEnumerable<PackageReference> packageReference) : base(path)
        {
            ProjectReferences = projectReferences;
            PackageReferences = packageReference;
        }

        public void Build()
        {
            BuildTargetFrameworks();
            DeleteIfExists();
            Create();
            Write();
        }

        private void BuildTargetFrameworks()
        {
            TargetFrameworks = Constats.DefaultTargetFrameworks;
            if (Path.Contains(string.Format("\\{0}\\", Constats.DefaultTestsFolderName)))
                TargetFrameworks = Constats.BaseTargetFrameworks;
        }

        private void DeleteIfExists()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }

        private void Create()
        {
            using var stream = File.Create(Path);
        }

        private void Write()
        {
            var content = GetContent();
            File.WriteAllText(Path, content);
        }

        private string GetContent()
        {
            StringBuilder builder = new StringBuilder();
            {
                builder.AppendFormat(@"
<Project Sdk=""Microsoft.NET.Sdk"">");

                builder.AppendLine();

                builder.AppendFormat(@"
	<PropertyGroup>
		<TargetFrameworks>{0}</TargetFrameworks>
	</PropertyGroup>", string.Join(";", TargetFrameworks));

                if (PackageReferences != null && PackageReferences.Any())
                {
                    builder.AppendLine();

                    builder.AppendFormat(@"
    <ItemGroup>");

                    foreach (var package in PackageReferences)
                    {
                        builder.AppendFormat(@"
        <PackageReference Include=""{0}"" Version=""{1}"" />", package.Name, package.Version);
                    }
                    builder.AppendFormat(@"
    </ItemGroup>");
                }

                if (ProjectReferences != null && ProjectReferences.Any())
                {
                    builder.AppendLine();

                    builder.AppendFormat(@"
    <ItemGroup>");
                    foreach (var project in ProjectReferences)
                    {
                        builder.AppendFormat(@"
        <ProjectReference Include=""{0}"" />", project);
                    }
                    builder.AppendFormat(@"
    </ItemGroup>");
                }

                builder.AppendFormat(@"
</Project>
");
            }
            return builder.ToString();
        }
    }

    public class PackageReference
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public static class Constats
    {
        public readonly static IEnumerable<string> DefaultTargetFrameworks = new List<string>() { "netstandard2.0", "netstandard2.1", "net461", "net5.0" };
        public readonly static IEnumerable<string> BaseTargetFrameworks = new List<string>() { "net5.0" };
        public readonly static string TestSolutionKey = "test";
        public readonly static string DefaultTestsFolderName = "tests";
        public static readonly string TestSolutionDirectoryPath = @"E:\Projects\tv.web.libraries.deployment";
    }
}
