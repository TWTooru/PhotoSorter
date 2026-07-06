# EXIF Extraction Optimization Design

## 1. Overview
The current implementation of `MainPresenter` uses `System.Drawing.Image.FromStream` to load the entire image into memory just to extract EXIF metadata (camera model, shutter speed, aperture, ISO, focal length). This causes a severe performance bottleneck, as the image is decoded twice per viewing (once in the `Form1` View for display, and once in the Presenter for metadata). The goal is to optimize this by using `MetadataExtractor`, which reads only the metadata headers and skips the pixel decoding process.

## 2. Architecture & Approach
We will utilize the **`MetadataExtractor`** NuGet package, which is a lightweight and extremely fast library for reading EXIF and other image metadata.

- **Presenter Layer (`MainPresenter.cs`)**:
  - The heavy `Image.FromStream` call in `ExtractAndSendExif` will be replaced with `ImageMetadataReader.ReadMetadata(filePath)`.
  - The custom, low-level byte-parsing methods (`GetExifString`, `GetExifShort`, `GetExifRational`) will be entirely removed, simplifying the codebase.
  - The presenter will extract the required fields using `ExifIfd0Directory` (for Camera Make/Model) and `ExifSubIfdDirectory` (for Exposure Time, F-Number, ISO, Focal Length).
  
- **View Layer (`Form1.cs`)**:
  - The View's core responsibility is rendering the image on the screen, which requires full pixel decoding (`Image.FromStream`).
  - Currently, the View reads the EXIF Orientation flag (`0x0112`) from the loaded `Image` object to automatically rotate the photo. Since the image is already fully loaded in memory at this stage, reading this property has near-zero performance cost.
  - To minimize risk and keep the View's rendering logic self-contained, the View's image loading and rotation logic will remain **unchanged**.

## 3. Implementation Details

### 3.1 Dependencies
- Install the `MetadataExtractor` package via NuGet into the `photo` project.

### 3.2 Refactoring `MainPresenter.cs`
- Add `using MetadataExtractor;` and `using MetadataExtractor.Formats.Exif;`.
- Refactor `ExtractAndSendExif` to:
  ```csharp
  var directories = ImageMetadataReader.ReadMetadata(filePath);
  var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
  var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
  ```
- Map the required string format:
  - Model: `ifd0?.GetDescription(ExifDirectoryBase.TagMake)` + `ifd0?.GetDescription(ExifDirectoryBase.TagModel)`
  - Shutter: `subIfd?.GetDescription(ExifDirectoryBase.TagExposureTime)`
  - Aperture: `subIfd?.GetDescription(ExifDirectoryBase.TagFNumber)`
  - ISO: `subIfd?.GetDescription(ExifDirectoryBase.TagIsoEquivalent)`
  - Focal Length: `subIfd?.GetDescription(ExifDirectoryBase.TagFocalLength)`
- Ensure error handling (`try-catch`) remains to gracefully fall back to "無 EXIF 資訊" (No EXIF Info) if reading fails or files are corrupted.

## 4. Testing Strategy
- Update existing unit tests in `MainPresenterTests.cs` if necessary. (Note: EXIF extraction interacts directly with files. If the original presenter used physical files to test EXIF or skipped it, we should ensure the tests still compile and run. Since `ExtractAndSendExif` is a private method triggered by a public event, its effects should be validated via the `_view.UpdateExifData` mock).
- Manual validation: Open a folder of large RAW-paired JPGs to visually verify that the delay between pressing next/previous is noticeably reduced and EXIF data is still correctly displayed in the status bar.
