using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using photo.Core.Interfaces;
using photo.Core.Models;
using photo.Core.Presenters;

namespace photo.Tests.Core.Presenters
{
    public class MainPresenterTests
    {
        [Fact]
        public async Task NextRequested_AdvancesIndexAndUpdatesView()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg", "C:\\test\\2.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            Assert.Equal(1, session.CurrentIndex);
            mockView.Verify(v => v.DisplayImage(It.IsAny<Image?>(), "2.jpg"), Times.Once);
            mockView.Verify(v => v.UpdateStatus(2, 2, "2.jpg"), Times.Once);
        }

        [Fact]
        public void PurgeRequested_CallsPurgeOnSession()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            mockFs.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), "*.*")).Returns(new[] { "temp1.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.PurgeRequested += null, EventArgs.Empty);
            
            mockFs.Verify(fs => fs.DeleteFile("temp1.jpg"), Times.Exactly(3)); // 3 folders checked in Purge()
            mockView.Verify(v => v.ShowNotification(It.IsAny<string>(), It.IsAny<System.Drawing.Color>()), Times.Once);
        }

        [Fact]
        public void RemoveRawRequested_CallsRemoveRawOnSession()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            mockFs.Setup(fs => fs.DirectoryExists("C:\\test\\raw")).Returns(true);
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test\\raw", "*.*")).Returns(new[] { "C:\\test\\raw\\1.raw", "C:\\test\\raw\\2.raw" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.RemoveRawRequested += null, EventArgs.Empty);
            
            // 2.raw should be removed, 1.raw should be kept because 1.jpg exists
            mockFs.Verify(fs => fs.SendToRecycleBin("C:\\test\\raw\\2.raw"), Times.Once);
            mockFs.Verify(fs => fs.SendToRecycleBin("C:\\test\\raw\\1.raw"), Times.Never);
        }

        [Fact]
        public async Task RotateRequested_UpdatesSessionRotationAndUpdatesView()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.RotateRequested += null, mockView.Object, true);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            Assert.Equal(1, session.GetRotation("C:\\test\\1.jpg"));
            mockView.Verify(v => v.DisplayImage(It.IsAny<Image?>(), "1.jpg"), Times.Once);
        }

        [Fact]
        public async Task MoveToStagingRequested_MovesFilesAndRecordsUndo()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            mockFs.Setup(fs => fs.DirectoryExists("C:\\test\\raw")).Returns(true);
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test\\raw", "1.*")).Returns(new[] { "C:\\test\\raw\\1.raw" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.MoveToStagingRequested += null, EventArgs.Empty);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            mockFs.Verify(fs => fs.MoveFile("C:\\test\\1.jpg", "C:\\test\\JPG暫存\\1.jpg"), Times.Once);
            mockFs.Verify(fs => fs.MoveFile("C:\\test\\raw\\1.raw", "C:\\test\\raw暫存\\1.raw"), Times.Once);
            Assert.Empty(session.JpgFiles);
            Assert.Single(session.UndoStack);
        }

        [Fact]
        public async Task DeleteRequested_SendsToRecycleBin()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            mockFs.Setup(fs => fs.DirectoryExists("C:\\test\\raw")).Returns(true);
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test\\raw", "1.*")).Returns(new[] { "C:\\test\\raw\\1.raw" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.DeleteRequested += null, EventArgs.Empty);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            mockFs.Verify(fs => fs.SendToRecycleBin("C:\\test\\1.jpg"), Times.Once);
            mockFs.Verify(fs => fs.SendToRecycleBin("C:\\test\\raw\\1.raw"), Times.Once);
            Assert.Empty(session.JpgFiles);
        }

        [Fact]
        public async Task UndoRequested_RevertsMoveToStaging()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            mockFs.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            // First move to staging
            mockView.Raise(v => v.MoveToStagingRequested += null, EventArgs.Empty);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            // Then undo
            mockView.Raise(v => v.UndoRequested += null, EventArgs.Empty);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            mockFs.Verify(fs => fs.MoveFile("C:\\test\\JPG暫存\\1.jpg", "C:\\test\\1.jpg"), Times.Once);
            Assert.Single(session.JpgFiles);
            Assert.Empty(session.UndoStack);
        }

        [Fact]
        public async Task ExtractAndSendExif_SendsFallbackString_WhenFileDoesNotExist()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(() => new System.IO.MemoryStream());
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\missing.jpg" });
            mockFs.Setup(fs => fs.EnumerateFiles("C:\\test", "*.jpeg")).Returns(Array.Empty<string>());
            mockFs.Setup(fs => fs.FileExists("C:\\test\\missing.jpg")).Returns(false);
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            if (presenter.LastLoadTask != null) await presenter.LastLoadTask;
            
            mockView.Verify(v => v.UpdateExifData("無 EXIF 資訊"), Times.Once);
        }
    }
}
