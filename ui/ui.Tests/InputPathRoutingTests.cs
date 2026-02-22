using BigZipUI.Services;
using BigZipUI.Tests.Helpers;
using BigZipUI.ViewModels;
using Moq;
using Xunit;

namespace BigZipUI.Tests
{
    public class InputPathRoutingTests
    {
        private static MainWindowViewModel CreateVm() =>
            new(new Mock<IBigzipService>().Object, new SynchronousDispatcher());

        [Fact]
        public void NonBigzipInput_AppendsExtensionToOutputPath_AndSetsUnbigzipFalse()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\files\photo.jpg";

            Assert.Equal(@"C:\files\photo.jpg.bigzip", vm.OutputPath);
            Assert.False(vm.Unbigzip);
        }

        [Fact]
        public void BigzipInput_StripsExtensionFromOutputPath_AndSetsUnbigzipTrue()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\files\archive.bigzip";

            Assert.Equal(@"C:\files\archive", vm.OutputPath);
            Assert.True(vm.Unbigzip);
        }

        [Fact]
        public void BigzipInput_CaseInsensitive()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\files\archive.BIGZIP";

            Assert.Equal(@"C:\files\archive", vm.OutputPath);
            Assert.True(vm.Unbigzip);
        }

        [Fact]
        public void ManuallyChangedOutputPath_IsNotOverwrittenOnSubsequentInputChange()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\files\photo.jpg";
            vm.OutputPath = @"C:\custom\output.bigzip";

            vm.InputPath = @"C:\files\photo2.jpg";

            Assert.Equal(@"C:\custom\output.bigzip", vm.OutputPath);
        }

        [Fact]
        public void ToggleUnbigzipTrue_WithNonBigzipInput_SetsOutputPathToInputPath()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\files\photo.jpg";
            vm.Unbigzip = true;

            Assert.Equal(@"C:\files\photo.jpg", vm.OutputPath);
        }

        [Fact]
        public void ToggleUnbigzipFalse_SetsOutputPathToInputPlusExtension()
        {
            var vm = CreateVm();
            vm.InputPath = @"C:\files\archive.bigzip";
            vm.Unbigzip = false;

            Assert.Equal(@"C:\files\archive.bigzip.bigzip", vm.OutputPath);
        }

        [Fact]
        public void EmptyInputPath_DoesNotChangeOutputPath()
        {
            var vm = CreateVm();
            vm.OutputPath = @"C:\existing\output.bigzip";
            vm.InputPath = "";

            Assert.Equal(@"C:\existing\output.bigzip", vm.OutputPath);
        }
    }
}
