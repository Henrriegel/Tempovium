using System;
using System.Diagnostics;
using System.IO;

namespace Tempovium.Services;

public class MpvProcessService
{
    private Process? _process;

    public string IpcServerPath { get; }

    public MpvProcessService()
    {
        var tempDirectory = Path.GetTempPath();
        IpcServerPath = Path.Combine(tempDirectory, $"tempovium-mpv-{Guid.NewGuid():N}.sock");
    }

    public void Play(string filePath)
    {
        Play(filePath, 0);
    }

    public void Play(string filePath, nint windowHandle)
    {
        Stop();

        if (string.IsNullOrWhiteSpace(filePath))
            return;

        TryDeleteExistingSocket();

        var startInfo = new ProcessStartInfo
        {
            FileName = "mpv",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("--force-window=yes");
        startInfo.ArgumentList.Add("--idle=no");
        startInfo.ArgumentList.Add($"--input-ipc-server={IpcServerPath}");

        if (windowHandle != 0)
        {
            startInfo.ArgumentList.Add($"--wid={windowHandle}");
        }

        startInfo.ArgumentList.Add(filePath);

        _process = Process.Start(startInfo);
    }

    public void Stop()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(2000);
            }
        }
        catch
        {
        }
        finally
        {
            _process?.Dispose();
            _process = null;
            TryDeleteExistingSocket();
        }
    }

    private void TryDeleteExistingSocket()
    {
        try
        {
            if (File.Exists(IpcServerPath))
            {
                File.Delete(IpcServerPath);
            }
        }
        catch
        {
        }
    }
}