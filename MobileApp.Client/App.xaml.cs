using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using Microsoft.WindowsAzure.MobileServices;
using MobileApp.Client.Constants;
using MobileApp.Client.Services;
using MobileApp.Client.ViewModels;
using MobileApp.Client.Views;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;

namespace MobileApp.Client
{
    sealed partial class App
    {
        public static MobileServiceClient MobileClient = new MobileServiceClient("{app-url}");

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            SetupIoc();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs protocolArgs = args as ProtocolActivatedEventArgs;
                Frame content = Window.Current.Content as Frame;
                if (content.Content.GetType() == typeof(MainPage))
                {
                    content.Navigate(typeof(MainPage), protocolArgs.Uri);
                }
            }
            Window.Current.Activate();
            base.OnActivated(args);
        }

        private void SetupIoc()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            RegisterServices();
            RegisterViewModels();
        }

        private void RegisterServices()
        {
            var nav = new NavigationService();
            nav.Configure(Navigation.MainPage, typeof(MainPage));
            nav.Configure(Navigation.NotePage, typeof(NotePage));
            SimpleIoc.Default.Register<INavigationService>(() => nav);

            var noteService = new NoteService();
            SimpleIoc.Default.Register(() => noteService);
        }

        private void RegisterViewModels()
        {
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<NoteViewModel>(() => new NoteViewModel(), true);
        }

        public static async Task<bool> AuthenticateAsync()
        {
            bool success = false;

            var provider = MobileServiceAuthenticationProvider.Google;

            // Use the PasswordVault to securely store and access credentials.
            var vault = new PasswordVault();
            PasswordCredential credential = null;

            try
            {
                // Try to get an existing credential from the vault.
                credential = vault.FindAllByResource(provider.ToString()).FirstOrDefault();
            }
            catch (Exception)
            {
                // When there is no matching resource an error occurs, which we ignore.
            }

            if (credential != null)
            {
                // Create a user from the stored credentials.
                var user = new MobileServiceUser(credential.UserName);
                credential.RetrievePassword();
                user.MobileServiceAuthenticationToken = credential.Password;

                // Set the user from the stored credentials.
                App.MobileClient.CurrentUser = user;

                // Consider adding a check to determine if the token is 
                // expired, as shown in this post: http://aka.ms/jww5vp.

                success = true;
            }
            else
            {
                try
                {
                    // Login with the identity provider.
                    var user = await App.MobileClient.LoginAsync(provider, "mobilenotes");

                    // Create and store the user credentials.
                    credential = new PasswordCredential(provider.ToString(), user.UserId, user.MobileServiceAuthenticationToken);
                    vault.Add(credential);

                    success = true;
                }
                catch (MobileServiceInvalidOperationException)
                {
                    var dialog = new MessageDialog("You must log in. Login Required");
                    dialog.Commands.Add(new UICommand("OK"));
                    await dialog.ShowAsync();
                }
            }
            return success;
        }
    }
}
