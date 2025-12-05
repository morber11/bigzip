using System;
using System.Threading;
using System.Threading.Tasks;

namespace BigZipUI.Services
{
    public record BigzipOptions(
        string InputPath,
        string? OutputPath,
        bool Unbigzip,
        string Factor,
        string Mode,
        bool ForceOverwrite
    );

    public interface IBigzipService
    {
        Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(BigzipOptions options, CancellationToken cancellationToken, IProgress<double>? progress = null);
        string? ResolveExecutablePath();
    }
}
