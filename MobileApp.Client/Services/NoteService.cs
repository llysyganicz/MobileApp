using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using CommonServiceLocator;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using MobileApp.Client.Models;

namespace MobileApp.Client.Services
{
    public class NoteService
    {
        private readonly IMobileServiceSyncTable<Note> _noteTable;

        private MobileServiceUser _user;

        public NoteService()
        {
            var store = new MobileServiceSQLiteStore("notes.db"); // create local db if not exists
            store.DefineTable<Note>();                            // create table based on Note type
            App.MobileClient.SyncContext.InitializeAsync(store);  // associate the local store with the sync context
            _noteTable = App.MobileClient.GetSyncTable<Note>();
        }

        public async Task SyncNotesAsync()
        {
            ReadOnlyCollection<MobileServiceTableOperationError> syncErrors = null;

            try
            {
                await App.MobileClient.SyncContext.PushAsync();

                await _noteTable.PullAsync(
                    "notes" + App.MobileClient.CurrentUser.UserId, 
                    _noteTable.CreateQuery().Where(n => n.UserId == App.MobileClient.CurrentUser.UserId));
            }
            catch (MobileServicePushFailedException exc)
            {
                if (exc.PushResult != null)
                {
                    syncErrors = exc.PushResult.Errors;
                }
            }

            if (syncErrors != null)
            {
                foreach (var error in syncErrors)
                {
                    if (error.OperationKind == MobileServiceTableOperationKind.Update && error.Result != null)
                    {
                        // Update failed, revert to server's copy
                        await error.CancelAndUpdateItemAsync(error.Result);
                    }
                    else
                    {
                        // Discard local change
                        await error.CancelAndDiscardItemAsync();
                    }
                }
            }
        }

        public async Task<IEnumerable<Note>> GetNotesAsync()
        {
            await SyncNotesAsync();
            return await _noteTable.Where(x => x.UserId == App.MobileClient.CurrentUser.UserId).OrderBy(x => x.Title).ToEnumerableAsync();
        }

        public async Task AddNoteAsync(Note note)
        {
            await _noteTable.InsertAsync(note);
            await SyncNotesAsync();
        }

        public async Task UpdateNoteAsync(Note note)
        {
            await _noteTable.UpdateAsync(note);
            await SyncNotesAsync();
        }

        public async Task DeleteNoteAsync(Note note)
        {
            await _noteTable.DeleteAsync(note);
            await SyncNotesAsync();
        }
    }
}