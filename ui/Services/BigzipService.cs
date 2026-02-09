using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static BigZipUI.Constants;

namespace BigZipUI.Services
{
    public class BigzipService : IBigzipService
    {
        public string? ResolveExecutablePath()
        {
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CLI_EXECUTABLE_NAME);
            return File.Exists(exePath) ? exePath : null;
        }

        public async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(BigzipOptions opt, CancellationToken ct, IProgress<double>? progress = null)
        {
            var exePath = ResolveExecutablePath();
            if (exePath is null)
            {
                return (-1, string.Empty, CLI_EXECUTABLE_NAME + " not found");
            }


            progress?.Report(PROGRESS_INIT);

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (opt.Unbigzip)
            {
                startInfo.ArgumentList.Add("-uz");
            }

            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(opt.InputPath);

            if (!string.IsNullOrWhiteSpace(opt.OutputPath))
            {
                startInfo.ArgumentList.Add("-o");
                startInfo.ArgumentList.Add(opt.OutputPath!);
            }

            if (!opt.Unbigzip)
            {
                startInfo.ArgumentList.Add("-f");
                startInfo.ArgumentList.Add(opt.Factor);
                startInfo.ArgumentList.Add("-mode");
                startInfo.ArgumentList.Add(opt.Mode);
            }

            if (opt.ForceOverwrite)
            {
                startInfo.ArgumentList.Add("-force");
            }

            progress?.Report(PROGRESS_ARGS_READY);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (-1, string.Empty, "Failed to start process");
            }

            progress?.Report(PROGRESS_PROCESS_STARTED);

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var progressTask = Task.Run(async () =>
            {
                double currentProgress = PROGRESS_PROCESS_STARTED;
                try
                {
                    while (!progressCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(PROGRESS_UPDATE_DELAY_MS, progressCts.Token);
                        currentProgress = Math.Min(currentProgress + PROGRESS_INCREMENT, PROGRESS_MAX_INCREMENTAL);
                        progress?.Report(currentProgress);
                    }
                }
                catch (OperationCanceledException)
                {
                    //expected
                }
            }, progressCts.Token);

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                throw;
            }
            finally
            {
                progressCts.Cancel();
                try
                {
                    await progressTask;
                }
                catch (OperationCanceledException)
                {
                    // expected
                }
                progressCts.Dispose();

                if (!ct.IsCancellationRequested)
                {
                    progress?.Report(PROGRESS_COMPLETE);
                }
            }

            await Task.WhenAll(stdOutTask, stdErrTask);

            return (process.ExitCode, stdOutTask.Result, stdErrTask.Result);
        }
    }
}
