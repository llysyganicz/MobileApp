using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using MobileApp.Client.Constants;
using MobileApp.Client.Messages;
using MobileApp.Client.Models;

namespace MobileApp.Client.ViewModels
{
    public class NoteViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private Note _note;

        private RelayCommand _save;
        private RelayCommand _cancel;

        public NoteViewModel()
        {
            _note = new Note();
            _nav = ServiceLocator.Current.GetInstance<INavigationService>();
            Messenger.Default.Register<LoadNote>(this, m =>
            {
                _note = m.Note;
                RaisePropertyChanged(nameof(Title));
                RaisePropertyChanged(nameof(Content));
            });
        }

        public string Title
        {
            get => _note.Title;
            set
            {
                _note.Title = value;
                RaisePropertyChanged();
            }
        }

        public string Content
        {
            get => _note.Content;
            set
            {
                _note.Content = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand Save => _save ?? (_save = new RelayCommand(SaveExecute));

        private void SaveExecute()
        {
            Messenger.Default.Send(new SaveNoteMessage
            {
                Note = _note
            });
            NavigateToMainPage();
        }

        public RelayCommand Cancel => _cancel ?? (_cancel = new RelayCommand(CancelExecute));

        private void CancelExecute()
        {
            NavigateToMainPage();
        }

        private void NavigateToMainPage()
        {
            _note = new Note();
            _nav.NavigateTo(Navigation.MainPage);
        }
    }
}