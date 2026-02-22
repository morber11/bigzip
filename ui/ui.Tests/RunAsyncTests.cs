using BigZipUI.Services;
using BigZipUI.Tests.Helpers;
using BigZipUI.ViewModels;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BigZipUI.Tests
{
    public class RunAsyncTests
    {
        private readonly Mock<IBigzipService> _serviceMock = new();
        private readonly SynchronousDispatcher _dispatcher = new();

        private MainWindowViewModel CreateVm(
            Func<string, Task>? showDialog = null,
            Func<string, Task<bool>>? confirmDialog = null,
            Func<bool, string, Task>? showResultDialog = null) =>
            new(_serviceMock.Object, _dispatcher,
                showDialog: showDialog,
                confirmDialog: confirmDialog,
                showResultDialog: showResultDialog);

        [Fact]
        public async Task EmptyInputPath_WithNoShowDialog_SetsStatusMessageAndDoesNotCallService()
        {
            var vm = CreateVm();

            await vm.RunCommand.ExecuteAsync(null);

            Assert.Equal("Error: Input file is required", vm.StatusMessage);
            _serviceMock.Verify(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()), Times.Never);
        }

        [Fact]
        public async Task NonExistentInputFile_SetsStatusMessageAndDoesNotCallService()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\does\not\exist\file.dat";

            await vm.RunCommand.ExecuteAsync(null);

            Assert.Equal("Error: Input file does not exist", vm.StatusMessage);
            _serviceMock.Verify(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()), Times.Never);
        }

        [Fact]
        public async Task ResolveExecutableReturnsNull_SetsNotFoundStatusAndDoesNotCallRunAsync()
        {
            _serviceMock.Setup(s => s.ResolveExecutablePath()).Returns((string?)null);
            var vm = CreateVm();

            var inputFile = Path.GetTempFileName();
            try
            {
                vm.InputPath = inputFile;
                await vm.RunCommand.ExecuteAsync(null);

                Assert.Contains("not found", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
                _serviceMock.Verify(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()), Times.Never);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }

        [Fact]
        public async Task OutputFileExists_ConfirmDialogReturnsFalse_ServiceIsNeverCalled()
        {
            _serviceMock.Setup(s => s.ResolveExecutablePath()).Returns("bz.exe");
            var vm = CreateVm(confirmDialog: _ => Task.FromResult(false));

            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();
            try
            {
                vm.InputPath = inputFile;
                vm.OutputPath = outputFile;

                await vm.RunCommand.ExecuteAsync(null);

                _serviceMock.Verify(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()), Times.Never);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        [Fact]
        public async Task OutputFileExists_ConfirmDialogReturnsTrue_ServiceCalledWithForceOverwriteTrue()
        {
            _serviceMock.Setup(s => s.ResolveExecutablePath()).Returns("bz.exe");
            _serviceMock
                .Setup(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()))
                .ReturnsAsync((0, "Wrote result.bigzip (size: 1 bytes)", ""));

            BigzipOptions? capturedOptions = null;
            _serviceMock
                .Setup(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()))
                .Callback<BigzipOptions, CancellationToken, IProgress<double>?>((opts, _, _) => capturedOptions = opts)
                .ReturnsAsync((0, "Wrote result.bigzip (size: 1 bytes)", ""));

            var vm = CreateVm(confirmDialog: _ => Task.FromResult(true));

            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();
            try
            {
                vm.InputPath = inputFile;
                vm.OutputPath = outputFile;

                await vm.RunCommand.ExecuteAsync(null);

                Assert.NotNull(capturedOptions);
                Assert.True(capturedOptions!.ForceOverwrite);
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        [Fact]
        public async Task SuccessfulRun_CallsShowResultDialogWithTrueAndParsedPath_AndResetsState()
        {
            _serviceMock.Setup(s => s.ResolveExecutablePath()).Returns("bz.exe");
            _serviceMock
                .Setup(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()))
                .ReturnsAsync((0, "Wrote /out/archive.bigzip (size: 42 bytes)", ""));

            bool? resultSuccess = null;
            string? resultPath = null;
            var vm = CreateVm(showResultDialog: (success, path) =>
            {
                resultSuccess = success;
                resultPath = path;
                return Task.CompletedTask;
            });

            var inputFile = Path.GetTempFileName();
            try
            {
                vm.InputPath = inputFile;
                await vm.RunCommand.ExecuteAsync(null);

                Assert.True(resultSuccess);
                Assert.Equal("/out/archive.bigzip", resultPath);
                Assert.False(vm.IsRunning);
                Assert.False(vm.ProgressVisible);
                Assert.Equal(0, vm.Progress);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }

        [Fact]
        public async Task FailedRun_CallsShowResultDialogWithFalseAndStdErr_AndResetsState()
        {
            _serviceMock.Setup(s => s.ResolveExecutablePath()).Returns("bz.exe");
            _serviceMock
                .Setup(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()))
                .ReturnsAsync((1, "", "file format not supported"));

            bool? resultSuccess = null;
            string? resultMsg = null;
            var vm = CreateVm(showResultDialog: (success, msg) =>
            {
                resultSuccess = success;
                resultMsg = msg;
                return Task.CompletedTask;
            });

            var inputFile = Path.GetTempFileName();
            try
            {
                vm.InputPath = inputFile;
                await vm.RunCommand.ExecuteAsync(null);

                Assert.False(resultSuccess);
                Assert.Equal("file format not supported", resultMsg);
                Assert.False(vm.IsRunning);
                Assert.False(vm.ProgressVisible);
                Assert.Equal(0, vm.Progress);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }

        [Fact]
        public async Task ServiceThrowsOperationCancelled_SetsStatusCancelledAndResetsState()
        {
            _serviceMock.Setup(s => s.ResolveExecutablePath()).Returns("bz.exe");
            _serviceMock
                .Setup(s => s.RunAsync(It.IsAny<BigzipOptions>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<double>>()))
                .ThrowsAsync(new OperationCanceledException());

            var vm = CreateVm();

            var inputFile = Path.GetTempFileName();
            try
            {
                vm.InputPath = inputFile;
                await vm.RunCommand.ExecuteAsync(null);

                Assert.Equal("Cancelled", vm.StatusMessage);
                Assert.False(vm.IsRunning);
                Assert.False(vm.ProgressVisible);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }
    }
}
