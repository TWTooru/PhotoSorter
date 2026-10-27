using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Moq;
using Xunit;
using photo.Core.Interfaces;
using photo.Infrastructure;

namespace photo.Tests.Infrastructure
{
    public class ImageLoaderTests
    {
        [Fact]
        public async Task LoadAsync_FileNotFound_ReturnsNullAndFallbackExif()
        {
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.FileExists("missing.jpg")).Returns(false);

            var loader = new ImageLoader(mockFs.Object);
            var (image, exifData) = await loader.LoadAsync("missing.jpg", 0, CancellationToken.None);

            Assert.Null(image);
            Assert.Equal("無 EXIF 資訊", exifData);
        }

        [Fact]
        public async Task LoadAsync_EmptyFileBytes_ReturnsNullAndFallbackExif()
        {
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.FileExists("empty.jpg")).Returns(true);
            mockFs.Setup(fs => fs.OpenRead("empty.jpg")).Returns(() => new MemoryStream(Array.Empty<byte>()));

            var loader = new ImageLoader(mockFs.Object);
            var (image, exifData) = await loader.LoadAsync("empty.jpg", 0, CancellationToken.None);

            Assert.Null(image);
            Assert.Equal("無 EXIF 資訊", exifData);
        }

        // Real HEIC sample (libheif examples/example.heic). Magick.NET can decode
        // HEIC but ships no HEVC encoder, so we cannot generate one in-test.
        private static byte[] LoadSampleHeic() =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "sample.heic"));

        private static Mock<IFileSystemService> MockFileWithBytes(string path, byte[] bytes)
        {
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.FileExists(path)).Returns(true);
            mockFs.Setup(fs => fs.OpenRead(path)).Returns(() => new MemoryStream(bytes));
            return mockFs;
        }

        [Fact]
        public void MagickNet_SupportsReadingHeic()
        {
            var info = MagickFormatInfo.Create(MagickFormat.Heic);
            Assert.NotNull(info);
            Assert.True(info!.SupportsReading);
        }

        [Fact]
        public async Task LoadAsync_HeicFile_DecodesImage()
        {
            var mockFs = MockFileWithBytes("photo.heic", LoadSampleHeic());

            var loader = new ImageLoader(mockFs.Object);
            var (image, _) = await loader.LoadAsync("photo.heic", 0, CancellationToken.None);

            Assert.NotNull(image);
            Assert.True(image!.Width > 0 && image.Height > 0);
        }

        [Fact]
        public async Task LoadAsync_HeicFileWithUserRotation_RotatesImage()
        {
            var bytes = LoadSampleHeic();
            var loader = new ImageLoader(MockFileWithBytes("photo.heic", bytes).Object);

            var (upright, _) = await loader.LoadAsync("photo.heic", 0, CancellationToken.None);
            var (rotated, _) = await loader.LoadAsync("photo.heic", 1, CancellationToken.None);

            Assert.NotNull(upright);
            Assert.NotNull(rotated);
            Assert.Equal(upright!.Width, rotated!.Height);
            Assert.Equal(upright.Height, rotated.Width);
        }

        [Fact]
        public void IsHeif_DetectsByFtypBrandRegardlessOfExtension()
        {
            Assert.True(ImageLoader.IsHeif(LoadSampleHeic(), "misnamed.jpg"));
        }

        [Theory]
        [InlineData("a.heic", true)]
        [InlineData("a.HEIF", true)]
        [InlineData("a.jpg", false)]
        public void IsHeif_FallsBackToExtension(string path, bool expected)
        {
            Assert.Equal(expected, ImageLoader.IsHeif(new byte[] { 0xFF, 0xD8, 0xFF }, path));
        }
    }
}
