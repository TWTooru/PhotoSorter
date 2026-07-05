using System;
using Xunit;
using Moq;
using System.Collections.Generic;
using photo.Core.Interfaces;
using photo.Core.Models;

namespace photo.Tests.Core.Models
{
    public class PhotoSessionTests
    {
        private void SetupMocks(Mock<IFileSystemService> mockFs, string path, string[] jpgs)
        {
            mockFs.Setup(fs => fs.EnumerateFiles(path, "*.*")).Returns(Array.Empty<string>());
            mockFs.Setup(fs => fs.EnumerateFiles(path, "*.jpg")).Returns(jpgs);
            mockFs.Setup(fs => fs.EnumerateFiles(path, "*.jpeg")).Returns(Array.Empty<string>());
        }

        [Fact]
        public void InitializeWorkspace_LoadsJpgsAndSetsCurrentIndex()
        {
            var mockFs = new Mock<IFileSystemService>();
            SetupMocks(mockFs, "C:\\test", new[] { "C:\\test\\1.jpg", "C:\\test\\2.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            Assert.Equal(2, session.JpgFiles.Count);
            Assert.Equal(0, session.CurrentIndex);
            mockFs.Verify(fs => fs.CreateDirectory("C:\\test\\raw"), Times.Once);
        }

        [Fact]
        public void InitializeWorkspace_NullFolderPath_ThrowsArgumentNullException()
        {
            var mockFs = new Mock<IFileSystemService>();
            var session = new PhotoSession(mockFs.Object);

            Assert.Throws<ArgumentNullException>(() => session.InitializeWorkspace(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void InitializeWorkspace_EmptyFolderPath_ThrowsArgumentException(string folderPath)
        {
            var mockFs = new Mock<IFileSystemService>();
            var session = new PhotoSession(mockFs.Object);

            Assert.Throws<ArgumentException>(() => session.InitializeWorkspace(folderPath));
        }

        [Fact]
        public void InitializeWorkspace_EmptyDirectory_SetsCurrentIndexToMinusOne()
        {
            var mockFs = new Mock<IFileSystemService>();
            SetupMocks(mockFs, "C:\\test", Array.Empty<string>());
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            Assert.Empty(session.JpgFiles);
            Assert.Equal(-1, session.CurrentIndex);
        }

        [Fact]
        public void Next_IncrementsIndex_WhenNotAtEnd()
        {
            var mockFs = new Mock<IFileSystemService>();
            SetupMocks(mockFs, "C:\\test", new[] { "1.jpg", "2.jpg" });
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            session.Next();
            
            Assert.Equal(1, session.CurrentIndex);
        }

        [Fact]
        public void Next_DoesNotIncrementIndex_WhenAtEnd()
        {
            var mockFs = new Mock<IFileSystemService>();
            SetupMocks(mockFs, "C:\\test", new[] { "1.jpg" });
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            session.Next();
            
            Assert.Equal(0, session.CurrentIndex);
        }

        [Fact]
        public void Previous_DecrementsIndex_WhenNotAtBeginning()
        {
            var mockFs = new Mock<IFileSystemService>();
            SetupMocks(mockFs, "C:\\test", new[] { "1.jpg", "2.jpg" });
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            session.Next();
            session.Previous();
            
            Assert.Equal(0, session.CurrentIndex);
        }

        [Fact]
        public void Previous_DoesNotDecrementIndex_WhenAtBeginning()
        {
            var mockFs = new Mock<IFileSystemService>();
            SetupMocks(mockFs, "C:\\test", new[] { "1.jpg", "2.jpg" });
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            session.Previous();
            
            Assert.Equal(0, session.CurrentIndex);
        }
    }
}
