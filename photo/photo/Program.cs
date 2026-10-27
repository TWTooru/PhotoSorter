using photo.Core.Models;
using photo.Core.Presenters;
using photo.Infrastructure;

namespace photo
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            var fileSystem = new FileSystemService();
            var session = new PhotoSession(fileSystem);
            var imageLoader = new ImageLoader(fileSystem);

            using var form = new Form1();
            using var presenter = new MainPresenter(form, session, imageLoader);
            
            Application.Run(form);
        }
    }
}