using Xunit;
using Moq;
using System.Collections.Generic;
using photo.Core.Interfaces;
using photo.Core.Models;

namespace photo.Tests.Core.Models
{
    public class PhotoSessionTests
    {
        [Fact]
        public void InitializeWorkspace_LoadsJpgsAndSetsCurrentIndex()
        {
            var mockFs = new Mock<IFileSystemService>();
            mockFs.Setup(fs => fs.GetFiles("C:\\test", "*.jpg")).Returns(new[] { "C:\\test\\1.jpg", "C:\\test\\2.jpg" });
            
            var session = new PhotoSession(mockFs.Object);
            session.InitializeWorkspace("C:\\test");
            
            Assert.Equal(2, session.JpgFiles.Count);
            Assert.Equal(0, session.CurrentIndex);
            mockFs.Verify(fs => fs.CreateDirectory("C:\\test\\raw"), Times.Once);
        }
    }
}
