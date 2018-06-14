using CommonServiceLocator;

namespace MobileApp.Client.ViewModels
{
    public class ViewModelLocator
    {
        public MainViewModel MainViewModel => ServiceLocator.Current.GetInstance<MainViewModel>();
        public NoteViewModel NoteViewModel => ServiceLocator.Current.GetInstance<NoteViewModel>();
    }
}