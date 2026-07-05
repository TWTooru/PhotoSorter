using Xunit;
using Moq;
using System;
using photo.Core.Interfaces;
using photo.Core.Models;
using photo.Core.Presenters;

namespace photo.Tests.Core.Presenters
{
    public class MainPresenterTests
    {
        [Fact]
        public void NextRequested_AdvancesIndexAndUpdatesView()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg", "C:\\test\\2.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test"); // sets current index to 0
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            // Trigger Next
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            
            Assert.Equal(1, session.CurrentIndex);
            mockView.Verify(v => v.DisplayImage("C:\\test\\2.jpg", false), Times.Once);
        }

        [Fact]
        public void NextRequested_IndexOutOfBounds_DoesNotDisplayImage()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test"); // sets current index to 0
            
            // Force out of bounds by clearing the list while index is 0
            session.JpgFiles.Clear();
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            // Trigger Next
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            
            mockView.Verify(v => v.DisplayImage(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void NextRequested_EmptyWorkspace_DoesNotDisplayImage()
        {
            var mockView = new Mock<IMainView>();
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(Array.Empty<string>());
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test"); // sets current index to -1
            
            var presenter = new MainPresenter(mockView.Object, session);
            
            // Trigger Next
            mockView.Raise(v => v.NextRequested += null, EventArgs.Empty);
            
            mockView.Verify(v => v.DisplayImage(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
