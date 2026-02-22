using BigZipUI.Services;
using BigZipUI.Tests.Helpers;
using BigZipUI.ViewModels;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BigZipUI.Tests
{
    public class BrowseCommandTests
    {
        private static MainWindowViewModel CreateVm(
            Func<Task<string?>>? openPicker = null,
            Func<Task<string?>>? savePicker = null) =>
            new(new Mock<IBigzipService>().Object, new SynchronousDispatcher(),
                openPicker: openPicker, savePicker: savePicker);

        [Fact]
        public async Task BrowseInput_NullPicker_DoesNotChangeInputPath()
        {
            var vm = CreateVm(openPicker: null);
            await vm.BrowseInputCommand.ExecuteAsync(null);

            Assert.Equal(string.Empty, vm.InputPath);
        }

        [Fact]
        public async Task BrowseInput_PickerReturnsPath_SetsInputPath()
        {
            var vm = CreateVm(openPicker: () => Task.FromResult<string?>(@"C:\files\photo.jpg"));
            await vm.BrowseInputCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\files\photo.jpg", vm.InputPath);
        }

        [Fact]
        public async Task BrowseInput_PickerReturnsNull_DoesNotChangeInputPath()
        {
            var vm = CreateVm(openPicker: () => Task.FromResult<string?>(null));
            await vm.BrowseInputCommand.ExecuteAsync(null);

            Assert.Equal(string.Empty, vm.InputPath);
        }

        [Fact]
        public async Task BrowseInput_PickerThrows_SetsStatusMessageWithError()
        {
            var vm = CreateVm(openPicker: () => throw new Exception("disk error"));
            await vm.BrowseInputCommand.ExecuteAsync(null);

            Assert.StartsWith("Error:", vm.StatusMessage);
        }

        [Fact]
        public async Task BrowseOutput_PickerReturnsPath_SetsOutputPath()
        {
            var vm = CreateVm(savePicker: () => Task.FromResult<string?>(@"C:\out\result.bigzip"));
            await vm.BrowseOutputCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\out\result.bigzip", vm.OutputPath);
        }
    }
}
