using System;
using photo.Core.Interfaces;
using photo.Core.Models;

namespace photo.Core.Presenters
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly PhotoSession _session;

        public MainPresenter(IMainView view, PhotoSession session)
        {
            _view = view;
            _session = session;

            _view.NextRequested += OnNextRequested;
        }

        private void OnNextRequested(object? sender, EventArgs e)
        {
            // Note: Since Task 2 implemented `Next()` and made CurrentIndex setter private,
            // we use the encapsulated method.
            _session.Next();
            
            if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.JpgFiles.Count)
            {
                _view.DisplayImage(_session.JpgFiles[_session.CurrentIndex], false);
            }
        }
    }
}
