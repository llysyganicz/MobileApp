using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.Web.Http.Filters;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using Microsoft.WindowsAzure.MobileServices;
using MobileApp.Client.Constants;
using MobileApp.Client.Messages;
using MobileApp.Client.Models;
using MobileApp.Client.Services;

namespace MobileApp.Client.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly NoteService _noteService;

        private ObservableCollection<Note> _notes;
        private Note _selectedNote;
        private RelayCommand _addNote;
        private RelayCommand<Note> _deleteNote;
        private RelayCommand _login;

        public MainViewModel()
        {
            _nav = ServiceLocator.Current.GetInstance<INavigationService>();
            _noteService = ServiceLocator.Current.GetInstance<NoteService>();

            Messenger.Default.Register<SaveNoteMessage>(this, SaveNote);
        }

        public ObservableCollection<Note> Notes
        {
            get
            {
                if (_notes == null)
                {
                    ReloadNotes().ContinueWith(x =>
                    {
                        if (App.MobileClient.CurrentUser == null) return;
                        _notes = x.Result;
                        RaisePropertyChanged(nameof(Notes));
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                    _notes = new ObservableCollection<Note>();
                }

                return _notes;
            }
            set
            {
                _notes = value;
                RaisePropertyChanged();
            }
        }

        public Note SelectedNote
        {
            get => _selectedNote;
            set
            {
                _selectedNote = value;
                EditNote();
                _selectedNote = null;
            }
        }

        public RelayCommand AddNote => _addNote ?? (_addNote = new RelayCommand(AddNoteExecute));

        private void AddNoteExecute()
        {
            _nav.NavigateTo(Navigation.NotePage);
        }

        public RelayCommand<Note> DeleteNote => _deleteNote ?? (_deleteNote = new RelayCommand<Note>(DeleteNoteExecute));

        private async void DeleteNoteExecute(Note note)
        {
            var noteToDelete = Notes.FirstOrDefault(n => n.Id == note.Id);
            if (noteToDelete != null)
            {
                Notes.Remove(noteToDelete);

                if (App.MobileClient.CurrentUser != null)
                    await _noteService.DeleteNoteAsync(noteToDelete);
            }
            RaisePropertyChanged(nameof(Notes));
        }

        private void EditNote()
        {
            Messenger.Default.Send(new LoadNote { Note = SelectedNote });
            _nav.NavigateTo(Navigation.NotePage);
        }

        private async void SaveNote(SaveNoteMessage message)
        {
            if (App.MobileClient.CurrentUser == null) App.AuthenticateAsync();

            if (message.Note.Id != null)
            {
                var editedNote = Notes.FirstOrDefault(note => note.Id == message.Note.Id);
                if (editedNote != null)
                {
                    editedNote.Title = message.Note.Title;
                    editedNote.Content = message.Note.Content;
                    editedNote.UpdatedAt = new DateTimeOffset(DateTime.Now);

                    if (App.MobileClient.CurrentUser != null)
                        await _noteService.UpdateNoteAsync(editedNote);
                }
            }
            else
            {
                var note = message.Note;
                note.Id = Guid.NewGuid().ToString();
                note.CreatedAt = new DateTimeOffset(DateTime.Now);
                note.UserId = App.MobileClient.CurrentUser.UserId;
                Notes.Add(note);

                if (App.MobileClient.CurrentUser != null)
                    await _noteService.AddNoteAsync(note);
            }
            RaisePropertyChanged(nameof(Notes));
        }

        private async Task<ObservableCollection<Note>> ReloadNotes()
        {
            if (App.MobileClient.CurrentUser == null) await App.AuthenticateAsync();

            var result = await _noteService.GetNotesAsync();

            return new ObservableCollection<Note>(result);
        }
    }
}