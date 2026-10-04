using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aion2Tools.Services;

/// <summary>Runs one external program and hands over its output line by line as it arrives.</summary>
public static class ProcessRunner
{
    /// <summary>The program could not be started at all, as opposed to running and failing.</summary>
    public const int COULD_NOT_START = -1;

    public static async Task<int> Run(
        string fileName, IReadOnlyList<string> arguments, string workingDirectory, Action<string> onLine, CancellationToken token)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(fileName);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        startInfo.WorkingDirectory = workingDirectory;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

        using Process process = new Process();
        process.StartInfo = startInfo;
        object gate = new object();
        process.OutputDataReceived += (_, e) => Report(gate, onLine, e.Data);
        process.ErrorDataReceived += (_, e) => Report(gate, onLine, e.Data);

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            onLine(exception.Message);
            return COULD_NOT_START;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        try
        {
            await process.WaitForExitAsync(token);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            throw;
        }

        return process.ExitCode;
    }

    /// <summary>Output and error arrive on separate threads; one line at a time keeps callers lock-free.</summary>
    private static void Report(object gate, Action<string> onLine, string? lineOrNull)
    {
        if (lineOrNull is null)
        {
            return;
        }

        lock (gate)
        {
            onLine(lineOrNull);
        }
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(true);
        }
        catch (Exception)
        {
            // Already exited between the cancellation and this call.
        }
    }
}
