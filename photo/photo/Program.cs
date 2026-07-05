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
            var form = new Form1();
            var presenter = new MainPresenter(form, session);
            
            Application.Run(form);
            
            presenter.Dispose();
        }
    }
}