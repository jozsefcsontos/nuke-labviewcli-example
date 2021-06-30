using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.LabVIEWCLI;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.LabVIEWCLI.LabVIEWCLITasks;

// More information regarding to LabVIEW CLI operations:
// https://zone.ni.com/reference/en-XX/help/371361R-01/lvhowto/cli_running_operations/
// https://zone.ni.com/reference/en-XX/help/371361R-01/lvconcepts/cli_predefined_operations/
[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.UnitTests);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            // Note: Mass compile is not required for executing build specifications. This is just for demonstrating the ability.
            LabVIEWCLIMassCompile(s => s
                .SetDirectoryToCompile(SourceDirectory / "LabVIEWExample")
                .SetPortNumber(5001));

            LabVIEWCLIExecuteBuildSpec(s => s
                .SetProjectPath(SourceDirectory / "LabVIEWExample" / "LabVIEWExample.lvproj")
                .SetBuildSpecName("PackedLibraryExample")
                .SetLogToConsole(true)
                .SetVerbosity(LogVerbosity.Diagnostic)
                .SetPortNumber(5001));

            LabVIEWCLICloseLabVIEW(s => s
               .SetPortNumber(5001));
        });

    Target VIAnalyzerTests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            LabVIEWCLIRunVIAnalyzer(s => s
                .SetConfigPath(SourceDirectory / "LabVIEWExample" / "VIAnalyzerTests.viancfg")
                .SetReportPath(ArtifactsDirectory / "VIAnalyzerResults.html")
                .SetReportSaveType(ReportTypes.HTML)
                .SetPortNumber(5001));

            LabVIEWCLICloseLabVIEW(s => s
                .SetPortNumber(5001));
        });

    Target UnitTests => _ => _
        .DependsOn(VIAnalyzerTests)
        .Executes(() =>
        {
        });
}
