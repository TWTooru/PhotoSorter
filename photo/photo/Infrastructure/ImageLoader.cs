using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using photo.Core.Interfaces;

namespace photo.Infrastructure
{
    public class ImageLoader : IImageLoader
    {
        private readonly IFileSystemService _fileSystem;

        public ImageLoader(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task<(Image? Image, string ExifData)> LoadAsync(
            string filePath, int rotation, CancellationToken token)
        {
            return await Task.Run(() => LoadInternal(filePath, rotation, token), token);
        }

        private (Image? Image, string ExifData) LoadInternal(
            string filePath, int rotation, CancellationToken token)
        {
            if (!_fileSystem.FileExists(filePath))
                return (null, "無 EXIF 資訊");

            // === Single file read into memory ===
            byte[] fileBytes;
            using (var stream = _fileSystem.OpenRead(filePath))
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                fileBytes = ms.ToArray();
            }

            token.ThrowIfCancellationRequested();

            if (fileBytes.Length == 0)
                return (null, "無 EXIF 資訊");

            // === Extract EXIF from the buffered bytes ===
            string exifData;
            try
            {
                using var exifStream = new MemoryStream(fileBytes, writable: false);
                var directories = ImageMetadataReader.ReadMetadata(exifStream);
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

                string make = ifd0?.GetDescription(ExifDirectoryBase.TagMake) ?? "";
                string model = ifd0?.GetDescription(ExifDirectoryBase.TagModel) ?? "";
                string fullModel = $"{make} {model}".Trim();
                if (string.IsNullOrEmpty(fullModel)) fullModel = "Unknown";

                string shutter = subIfd?.GetDescription(ExifDirectoryBase.TagExposureTime) ?? "Unknown";
                string aperture = subIfd?.GetDescription(ExifDirectoryBase.TagFNumber)
                    ?.Replace("f/", "") ?? "Unknown";
                string iso = subIfd?.GetDescription(ExifDirectoryBase.TagIsoEquivalent) ?? "Unknown";
                string focal = subIfd?.GetDescription(ExifDirectoryBase.TagFocalLength)
                    ?.Replace(" mm", "") ?? "Unknown";

                exifData = $"{fullModel} | {shutter}s | f/{aperture} | ISO {iso} | {focal}mm";
            }
            catch
            {
                exifData = "無 EXIF 資訊";
            }

            token.ThrowIfCancellationRequested();

            // === Decode image from the same buffered bytes ===
            Image? result = null;
            try
            {
                using var tempImg = IsHeif(fileBytes, filePath)
                    ? DecodeHeif(fileBytes)
                    : DecodeWithGdi(fileBytes);

                token.ThrowIfCancellationRequested();

                // Apply user rotation
                for (int i = 0; i < rotation; i++)
                    tempImg.RotateFlip(RotateFlipType.Rotate90FlipNone);

                result = new Bitmap(tempImg);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Not a valid image or corrupted stream
            }

            return (result, exifData);
        }

        private static readonly string[] HeifBrands =
        {
            "heic", "heix", "heim", "heis", "hevc", "hevx", "hevm", "hevs", "mif1", "msf1"
        };

        /// <summary>
        /// Detects HEIF/HEIC content by its ISO-BMFF 'ftyp' box brand,
        /// falling back to the file extension.
        /// </summary>
        internal static bool IsHeif(byte[] bytes, string filePath)
        {
            if (bytes.Length >= 12 &&
                bytes[4] == (byte)'f' && bytes[5] == (byte)'t' &&
                bytes[6] == (byte)'y' && bytes[7] == (byte)'p')
            {
                string brand = System.Text.Encoding.ASCII.GetString(bytes, 8, 4);
                if (HeifBrands.Contains(brand)) return true;
            }

            string ext = Path.GetExtension(filePath);
            return ext.Equals(".heic", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".heif", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Decodes HEIF/HEIC via Magick.NET (bundled libheif), applying orientation,
        /// then hands a BMP buffer to GDI+ so the rest of the pipeline stays unchanged.
        /// </summary>
        private static Image DecodeHeif(byte[] fileBytes)
        {
            using var magick = new MagickImage(fileBytes);
            magick.AutoOrient();
            magick.Format = MagickFormat.Bmp;
            byte[] bmpBytes = magick.ToByteArray();

            using var bmpStream = new MemoryStream(bmpBytes, writable: false);
            using var decoded = Image.FromStream(bmpStream);
            return new Bitmap(decoded);
        }

        /// <summary>
        /// Decodes JPEG and other GDI+-supported formats, applying EXIF orientation.
        /// </summary>
        private static Image DecodeWithGdi(byte[] fileBytes)
        {
            using var imageStream = new MemoryStream(fileBytes, writable: false);
            using var tempImg = Image.FromStream(imageStream);

            // Apply EXIF orientation
            if (Array.IndexOf(tempImg.PropertyIdList, 0x0112) > -1)
            {
                int orientation = tempImg.GetPropertyItem(0x0112)!.Value![0];
                RotateFlipType rf = RotateFlipType.RotateNoneFlipNone;
                switch (orientation)
                {
                    case 3: rf = RotateFlipType.Rotate180FlipNone; break;
                    case 6: rf = RotateFlipType.Rotate90FlipNone; break;
                    case 8: rf = RotateFlipType.Rotate270FlipNone; break;
                }
                if (rf != RotateFlipType.RotateNoneFlipNone) tempImg.RotateFlip(rf);
            }

            // Detach from the stream so the caller owns an independent bitmap
            return new Bitmap(tempImg);
        }
    }
}
