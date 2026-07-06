### Task 2: Refactor MainPresenter

**Files:**
- Modify: `photo/photo/Core/Presenters/MainPresenter.cs`

**Interfaces:**
- Consumes: `MetadataExtractor` APIs
- Produces: `MainPresenter` with optimized `ExtractAndSendExif` method

- [ ] **Step 1: Write the failing tests**
*(Note: As we are replacing an internal private method's implementation, the existing tests verify the overall presenter behavior. We will write a small unit test to ensure the mock view still receives an Exif string when Next/Prev is pressed).*

Append this test to `photo/photo.Tests/Core/Presenters/MainPresenterTests.cs`:

```csharp
        [Fact]
        public void ExtractAndSendExif_SendsFallbackString_WhenFileDoesNotExist()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\missing.jpg" });
            mockFs.Setup(fs => fs.FileExists("C:\\test\\missing.jpg")).Returns(false);
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            
            mockView.Verify(v => v.UpdateExifData("無 EXIF 資訊"), Times.Once);
        }
```

- [ ] **Step 2: Run test to verify the baseline**
Run: `dotnet test photo/photo.Tests/photo.Tests.csproj`
Expected: PASS (This verifies the baseline fallback behavior is tested before we refactor the EXIF logic).

- [ ] **Step 3: Replace EXIF extraction logic**

In `photo/photo/Core/Presenters/MainPresenter.cs`, replace `ExtractAndSendExif` and remove the helper methods `GetExifString`, `GetExifShort`, `GetExifRational`.

Add to top:
```csharp
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Linq;
```

Replace `ExtractAndSendExif` with:
```csharp
        private void ExtractAndSendExif(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _view.UpdateExifData("無 EXIF 資訊");
                return;
            }

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

                string make = ifd0?.GetDescription(ExifDirectoryBase.TagMake) ?? "";
                string model = ifd0?.GetDescription(ExifDirectoryBase.TagModel) ?? "";
                string fullModel = $"{make} {model}".Trim();
                if (string.IsNullOrEmpty(fullModel)) fullModel = "Unknown";

                string shutter = subIfd?.GetDescription(ExifDirectoryBase.TagExposureTime) ?? "Unknown";
                string aperture = subIfd?.GetDescription(ExifDirectoryBase.TagFNumber)?.Replace("f/", "") ?? "Unknown";
                string iso = subIfd?.GetDescription(ExifDirectoryBase.TagIsoEquivalent) ?? "Unknown";
                string focal = subIfd?.GetDescription(ExifDirectoryBase.TagFocalLength)?.Replace(" mm", "") ?? "Unknown";

                _view.UpdateExifData($"{fullModel} | {shutter}s | f/{aperture} | ISO {iso} | {focal}mm");
            }
            catch
            {
                _view.UpdateExifData("無 EXIF 資訊");
            }
        }
```

Remove these methods from `MainPresenter.cs`:
- `GetExifString`
- `GetExifShort`
- `GetExifRational`

- [ ] **Step 4: Run tests to verify they still pass**

Run: `dotnet test photo/photo.Tests/photo.Tests.csproj`
Expected: PASS (All tests should still pass, proving our refactoring didn't break presenter flow).

- [ ] **Step 5: Commit**

```bash
git add photo/photo/Core/Presenters/MainPresenter.cs photo/photo.Tests/Core/Presenters/MainPresenterTests.cs
git commit -m "refactor: optimize EXIF extraction using MetadataExtractor"
```
