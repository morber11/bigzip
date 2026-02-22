using BigZipUI.ViewModels;
using Xunit;

namespace BigZipUI.Tests
{
    public class StaticHelpersTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("file.zip", false)]
        [InlineData("file", false)]
        [InlineData("file.bigzip", true)]
        [InlineData("file.BIGZIP", true)]
        [InlineData("path/to/file.bigzip", true)]
        public void IsBigzip_ReturnsExpectedResult(string? path, bool expected)
        {
            Assert.Equal(expected, MainWindowViewModel.IsBigzip(path));
        }

        [Theory]
        [InlineData("Wrote /out/file.bigzip (size: 123 bytes)", false, "/out/file.bigzip")]
        [InlineData("Restored original to /out/file.txt (mode: zero)", true, "/out/file.txt")]
        [InlineData("Some unexpected output line", false, "Some unexpected output line")]
        [InlineData("  trimmed line  ", false, "trimmed line")]
        [InlineData("Wrote /out/file.bigzip without size suffix", false, "Wrote /out/file.bigzip without size suffix")]
        [InlineData("Restored original to /out/file.txt without mode suffix", true, "Restored original to /out/file.txt without mode suffix")]
        public void ParseActualOutputPath_ReturnsExpectedPath(string stdOut, bool isUnbigzip, string expected)
        {
            Assert.Equal(expected, MainWindowViewModel.ParseActualOutputPath(stdOut, isUnbigzip));
        }
    }
}
