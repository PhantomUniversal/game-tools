using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>The app rebuilding itself from the checkout it was published from (pipeline-hub's Kiln does
/// the same): pull, publish beside the running executable, swap the files, start again.</summary>
public static class SelfUpdate
{
    private const string STAGING_FOLDER_NAME = ".update";
    private const string OLD_SUFFIX = ".old";
    private const string WAIT_ARGUMENT = "--wait-for";
    private const string PROJECT_PATH = "src/Aion2Tools/Aion2Tools.csproj";

    private static readonly TimeSpan WAIT_LIMIT = TimeSpan.FromSeconds(30);

    /// <summary>The aion2 folder of the checkout this executable was published from. Empty for a debug build.</summary>
    public static string SourceFolder { get; } = GetMetadata("Aion2SourceFolder");

    /// <summary>The last commit that touched that folder when it was published.</summary>
    public static string BuiltCommit { get; } = GetMetadata("Aion2SourceCommit");

    public static bool IsAvailable => SourceFolder.Length > 0 && File.Exists(Path.Combine(SourceFolder, PROJECT_PATH));

    private static string StagingFolder => Path.Combine(AppPaths.ExecutableFolder, STAGING_FOLDER_NAME);

    /// <summary>Commits to the aion2 folder upstream that this copy lacks. 0 when it cannot tell.</summary>
    public static async Task<int> CountNewCommits(CancellationToken token)
    {
        if (!IsAvailable || BuiltCommit.Length == 0)
        {
            return 0;
        }

        int fetched = await ProcessRunner.Run("git", new[] { "fetch", "--quiet" }, SourceFolder, _ => { }, token);
        if (fetched != 0)
        {
            return 0;
        }

        StringBuilder count = new StringBuilder();
        await ProcessRunner.Run(
            "git", new[] { "rev-list", "--count", BuiltCommit + "..@{u}", "--", "." }, SourceFolder, line => count.Append(line.Trim()), token);
        int commits;
        return int.TryParse(count.ToString(), out commits) ? commits : 0;
    }

    /// <summary>Pulls, and publishes into the staging folder when there is something new.</summary>
    public static async Task<UpdateResult> Build(Action<string> onLine, CancellationToken token)
    {
        try
        {
            return await BuildStaged(onLine, token);
        }
        catch (OperationCanceledException)
        {
            DeleteFolder(StagingFolder);
            throw;
        }
    }

    /// <summary>Moves the staged files over the running ones. A running exe cannot be overwritten but can be renamed.</summary>
    public static bool Install(Action<string> onLine)
    {
        try
        {
            foreach (string staged in Directory.GetFiles(StagingFolder))
            {
                string target = Path.Combine(AppPaths.ExecutableFolder, Path.GetFileName(staged));
                if (File.Exists(target))
                {
                    string old = target + OLD_SUFFIX;
                    File.Delete(old);
                    File.Move(target, old);
                    File.SetAttributes(old, File.GetAttributes(old) | FileAttributes.Hidden);
                }

                File.Move(staged, target);
            }

            DeleteFolder(StagingFolder);
            return true;
        }
        catch (Exception exception)
        {
            onLine(exception.Message);
            return false;
        }
    }

    /// <summary>Starts the new copy, which waits for this one to exit before it opens.</summary>
    public static void StartNewCopy()
    {
        string? pathOrNull = Environment.ProcessPath;
        if (pathOrNull is null)
        {
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo(pathOrNull);
        startInfo.ArgumentList.Add(WAIT_ARGUMENT);
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        startInfo.WorkingDirectory = AppPaths.ExecutableFolder;
        startInfo.UseShellExecute = false;
        Process.Start(startInfo);
    }

    /// <summary>Called first on start: waits out the copy that launched this one, then clears what the swap left.</summary>
    public static void FinishPrevious(string[] args)
    {
        int index = Array.IndexOf(args, WAIT_ARGUMENT);
        int processId;
        if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out processId))
        {
            WaitForExit(processId);
        }

        foreach (string old in Directory.GetFiles(AppPaths.ExecutableFolder, "*" + OLD_SUFFIX))
        {
            try
            {
                File.Delete(old);
            }
            catch (Exception)
            {
                // Still held by something; the next start clears it.
            }
        }
    }

    private static async Task<UpdateResult> BuildStaged(Action<string> onLine, CancellationToken token)
    {
        int pulled = await ProcessRunner.Run("git", new[] { "pull", "--ff-only" }, SourceFolder, onLine, token);
        if (pulled != 0)
        {
            return UpdateResult.Failed;
        }

        StringBuilder commit = new StringBuilder();
        await ProcessRunner.Run("git", new[] { "rev-list", "-1", "HEAD", "--", "." }, SourceFolder, line => commit.Append(line.Trim()), token);
        if (commit.Length > 0 && commit.ToString() == BuiltCommit)
        {
            return UpdateResult.UpToDate;
        }

        string project = Path.Combine(SourceFolder, PROJECT_PATH);
        string projectFolder = Path.GetDirectoryName(project)!;
        DeleteFolder(Path.Combine(projectFolder, "bin", "Release"));
        DeleteFolder(Path.Combine(projectFolder, "obj", "Release"));
        DeleteFolder(StagingFolder);

        Environment.SetEnvironmentVariable("DOTNET_CLI_FORCE_UTF8_ENCODING", "true");
        List<string> arguments = new List<string>
        {
            "publish", project, "-c", "Release", "-r", RuntimeInformation.RuntimeIdentifier, "--self-contained", "true",
            "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-o", StagingFolder, "-nologo",
        };
        int published = await ProcessRunner.Run("dotnet", arguments, SourceFolder, onLine, token);
        if (published != 0)
        {
            DeleteFolder(StagingFolder);
            return UpdateResult.Failed;
        }

        foreach (string symbols in Directory.GetFiles(StagingFolder, "*.pdb"))
        {
            File.Delete(symbols);
        }

        return UpdateResult.Built;
    }

    private static void WaitForExit(int processId)
    {
        try
        {
            using Process previous = Process.GetProcessById(processId);
            previous.WaitForExit(WAIT_LIMIT);
        }
        catch (ArgumentException)
        {
            // Already gone.
        }
    }

    private static void DeleteFolder(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
    }

    private static string GetMetadata(string key)
    {
        AssemblyMetadataAttribute? attributeOrNull = typeof(SelfUpdate).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(item => item.Key == key);
        if (attributeOrNull is null || attributeOrNull.Value is null)
        {
            return string.Empty;
        }

        return attributeOrNull.Value;
    }
}
