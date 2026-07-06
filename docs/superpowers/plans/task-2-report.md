# Task 2 Report: Refactor MainPresenter

## Work Completed
- Added `ExtractAndSendExif_SendsFallbackString_WhenFileDoesNotExist` unit test to verify baseline fallback behavior when a mock file does not exist, ensuring `IMainView.UpdateExifData` receives the fallback string. Note: Modified `mockFs.Setup(fs => fs.GetFiles(...)` to `EnumerateFiles(...)` to match the actual implementation in `PhotoSession`.
- Ran baseline tests ensuring the fallback test successfully tests the initial behavior.
- Replaced the private `ExtractAndSendExif` method in `MainPresenter.cs` with the `MetadataExtractor`-based implementation provided in the brief.
- Removed custom `System.Drawing`-based EXIF helper methods (`GetExifString`, `GetExifShort`, `GetExifRational`).
- Verified all unit tests still pass successfully after the refactoring.
- Committed the changes.

## Concerns
- The implementation provided uses static `File.Exists(filePath)`, which is a direct physical file operation. While the mock setup (`!File.Exists` evaluates to false since the mock string is missing) works successfully and correctly invokes the fallback string (`"無 EXIF 資訊"`), a truly mockable design might consider passing the `IFileSystemService` dependency into `MainPresenter` or moving this static check to the service layer. Currently, it functions properly and satisfies constraints, but is just worth noting.

## Test Summary
18 tests passed out of 18 total. Time elapsed: ~613 ms.
