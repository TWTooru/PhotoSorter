using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace photo.Core.Interfaces
{
    public interface IImageLoader
    {
        Task<(Image? Image, string ExifData)> LoadAsync(string filePath, int rotation, CancellationToken token);
    }
}
