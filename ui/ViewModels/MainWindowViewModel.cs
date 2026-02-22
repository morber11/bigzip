using BigZipUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static BigZipUI.Constants;

namespace BigZipUI.ViewModels
{
    public partial class MainWindowViewModel(IBigzipService service,
        IDispatcher? dispatcher = null,
        Func<Task<string?>>? openPicker = null,
        Func<Task<string?>>? savePicker = null,
        Func<string, Task>? showDialog = null,
        Func<string, Task<bool>>? confirmDialog = null,
        Func<bool, string, Task>? showResultDialog = null) : ObservableObject
    {
        [ObservableProperty]
        private string _inputPath = string.Empty;

        [ObservableProperty]
        private string _outputPath = string.Empty;

        [ObservableProperty]
        private int _selectedSizeIndex = 1; // default to Medium

        [ObservableProperty]
        private int _selectedModeIndex = 0; // default to repeat

        [NotifyPropertyChangedFor(nameof(ModeEnabled))]
        [ObservableProperty]
        private bool _unbigzip;

        public bool ModeEnabled => !Unbigzip;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private bool _progressVisible;

        private Func<Task<string?>>? _openPicker = openPicker;
        private Func<Task<string?>>? _savePicker = savePicker;
        private Func<string, Task>? _showDialog = showDialog;
        private Func<string, Task<bool>>? _confirmDialog = confirmDialog;
        private Func<bool, string, Task>? _showResultDialog = showResultDialog;

        private readonly IBigzipService _service = service;
        private readonly IDispatcher _dispatcher = dispatcher ?? new AvaloniaDispatcher();
        private CancellationTokenSource? _cts;

        private string _lastInputPath = string.Empty;

        private static readonly string[] _factors = ["32", "64", "128", "256", "512"];
        private static readonly string[] _modes = ["repeat", "zero", "random"];

        public void SetDialogs(
            Func<Task<string?>>? openPicker = null,
            Func<Task<string?>>? savePicker = null,
            Func<string, Task>? showDialog = null,
            Func<string, Task<bool>>? confirmDialog = null,
            Func<bool, string, Task>? showResultDialog = null)
        {
            if (openPicker is not null) _openPicker = openPicker;
            if (savePicker is not null) _savePicker = savePicker;
            if (showDialog is not null) _showDialog = showDialog;
            if (confirmDialog is not null) _confirmDialog = confirmDialog;
            if (showResultDialog is not null) _showResultDialog = showResultDialog;
        }

        partial void OnInputPathChanged(string value)
        {
            var previous = _lastInputPath;
            var isBigzip = IsBigzip(value);

            var prevAutoBigzip = previous + BIGZIP_EXTENSION;
            var prevAutoUnbig = Path.ChangeExtension(previous, null) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(OutputPath) ||
                (!string.IsNullOrEmpty(previous) && (OutputPath == prevAutoBigzip || OutputPath == prevAutoUnbig)))
            {
                if (isBigzip)
                    OutputPath = Path.ChangeExtension(value, null) ?? string.Empty;
                else
                    OutputPath = value + BIGZIP_EXTENSION;
            }

            Unbigzip = isBigzip;
            _lastInputPath = value;
        }

        partial void OnIsRunningChanged(bool value)
        {
            RunCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(RunButtonText));
        }

        partial void OnUnbigzipChanged(bool value)
        {
            if (!string.IsNullOrWhiteSpace(InputPath))
            {
                if (value)
                {
                    OutputPath = IsBigzip(InputPath) ? Path.ChangeExtension(InputPath, null) ?? string.Empty : InputPath;
                }
                else
                {
                    OutputPath = InputPath + BIGZIP_EXTENSION;
                }
            }
        }

        public string RunButtonText => IsRunning ? "Cancel" : "Run BigZip";


        [RelayCommand]
        private async Task BrowseInputAsync()
        {
            if (_openPicker is null)
            {
                return;
            }

            try
            {
                var path = await _openPicker();
                if (!string.IsNullOrEmpty(path))
                {
                    InputPath = path;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        [RelayCommand]
        private async Task BrowseOutputAsync()
        {
            if (_savePicker is null)
            {
                return;
            }
            try
            {
                var path = await _savePicker();
                if (!string.IsNullOrEmpty(path))
                {
                    OutputPath = path;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async Task ExecuteRunAsync()
        {
            var (isValid, forceOverwrite, outputExisted) = await ValidateInputsAsync();
            if (!isValid)
            {
                return;
            }

            var exePath = _service.ResolveExecutablePath();
            if (exePath is null)
            {
                StatusMessage = "Error: " + CLI_EXECUTABLE_NAME + " not found";
                return;
            }

            var factor = _factors[Math.Clamp(SelectedSizeIndex, 0, _factors.Length - 1)];
            var mode = _modes[Math.Clamp(SelectedModeIndex, 0, _modes.Length - 1)];

            var options = new BigzipOptions(InputPath, string.IsNullOrWhiteSpace(OutputPath) ? null : OutputPath, Unbigzip, factor, mode, forceOverwrite);

            PrepareExecution();

            var progress = new Progress<double>(value =>
            {
                _dispatcher.Post(() =>
                {
                    Progress = value * 100;
                    if (value < PROGRESS_PROCESS_STARTED)
                        StatusMessage = "Preparing...";
                    else if (value < PROGRESS_MAX_INCREMENTAL)
                        StatusMessage = "Processing...";
                    else
                        StatusMessage = "Finalizing...";
                });
            });

            try
            {
                _cts = new CancellationTokenSource();
                var result = await _service.RunAsync(options, _cts.Token, progress);

                if (result.ExitCode == 0)
                {
                    await HandleSuccessAsync(result);
                }
                else
                {
                    await HandleErrorAsync(result);
                }
            }
            catch (OperationCanceledException)
            {
                _dispatcher.Post(() => StatusMessage = "Cancelled");
                return;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                ResetExecutionState();
                _cts?.Dispose();
                _cts = null;
            }
        }

        [RelayCommand]
        private async Task RunAsync()
        {
            if (!IsRunning)
            {
                await ExecuteRunAsync();
                return;
            }

            _dispatcher.Post(() =>
            {
                IsRunning = false;
                ProgressVisible = false;
                Progress = 0;
                OnPropertyChanged(nameof(RunButtonText));
                StatusMessage = "Cancelling...";
            });

            _cts?.Cancel();
        }

        private async Task<(bool isValid, bool forceOverwrite, bool outputExisted)> ValidateInputsAsync()
        {
            bool forceOverwrite = false;
            bool outputExisted = false;

            if (string.IsNullOrWhiteSpace(InputPath))
            {
                if (_showDialog is not null)
                    await _showDialog("You must select a file first");
                else
                    StatusMessage = "Error: Input file is required";
                return (false, forceOverwrite, outputExisted);
            }

            if (!File.Exists(InputPath))
            {
                StatusMessage = "Error: Input file does not exist";
                return (false, forceOverwrite, outputExisted);
            }

            if (!string.IsNullOrWhiteSpace(OutputPath) && File.Exists(OutputPath))
            {
                outputExisted = true;
                if (_confirmDialog is not null)
                {
                    forceOverwrite = await _confirmDialog($"The file '{OutputPath}' already exists");
                    if (!forceOverwrite)
                        return (false, forceOverwrite, outputExisted);
                }
                else
                {
                    StatusMessage = "Error: Output file already exists and no confirmation dialog available";
                    return (false, forceOverwrite, outputExisted);
                }
            }

            return (true, forceOverwrite, outputExisted);
        }

        private void PrepareExecution()
        {
            _dispatcher.Post(() =>
            {
                IsRunning = true;
                ProgressVisible = true;
                Progress = 0;
                StatusMessage = "Starting operation...";
            });
        }

        private async Task HandleSuccessAsync((int ExitCode, string StdOut, string StdErr) result)
        {
            Progress = 1.0;
            StatusMessage = "Success: " + result.StdOut.Trim();

            string resultPath = ParseActualOutputPath(result.StdOut, Unbigzip);

            if (_showResultDialog is not null)
            {
                await _showResultDialog(true, resultPath);
            }
            else if (_showDialog is not null)
            {
                await _showDialog($"Finished: {resultPath}");
            }
        }

        private async Task HandleErrorAsync((int ExitCode, string StdOut, string StdErr) result)
        {
            StatusMessage = "Error: " + result.StdErr.Trim();
            if (_showResultDialog is not null)
            {
                await _showResultDialog(false, result.StdErr.Trim());
            }
            else if (_showDialog is not null)
            {
                await _showDialog("Error: " + result.StdErr.Trim());
            }
        }

        private void ResetExecutionState()
        {
            _dispatcher.Post(() =>
            {
                IsRunning = false;
                ProgressVisible = false;
                Progress = 0;
                OnPropertyChanged(nameof(RunButtonText));
            });
            _cts?.Dispose();
            _cts = null;
        }

        internal static bool IsBigzip(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return string.Equals(Path.GetExtension(path), BIGZIP_EXTENSION, StringComparison.OrdinalIgnoreCase);
        }

        internal static string ParseActualOutputPath(string stdOut, bool isUnbigzip)
        {
            var line = stdOut.Trim();
            if (isUnbigzip)
            {
                const string prefix = "Restored original to ";
                if (line.StartsWith(prefix))
                {
                    var start = prefix.Length;
                    var end = line.LastIndexOf(" (mode:");
                    if (end > start)
                    {
                        return line[start..end];
                    }
                }
            }
            else
            {
                const string prefix = "Wrote ";
                if (line.StartsWith(prefix))
                {
                    var start = prefix.Length;
                    var end = line.LastIndexOf(" (size:");
                    if (end > start)
                    {
                        return line[start..end];
                    }
                }
            }
            return line;
        }

    }
}
