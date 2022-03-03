using DownloadManager.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Zadatak2 {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application {

        internal JobManager Manager { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e) {
            Manager = await JobManager.Load(await JobManager.GetSerializationFile());
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null) {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false) {
                if (rootFrame.Content == null) {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Start Background Task
        /// </summary>
        /// <param name="args"></param>
        protected async override void OnBackgroundActivated(BackgroundActivatedEventArgs args) {
            base.OnBackgroundActivated(args);
            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            var deferral = taskInstance.GetDeferral();
            await DoBackgroundWork();
            deferral.Complete();
        }

        /// <summary>
        /// Slanje obavjestenja o postojanju poslova koji nisu zavrseni
        /// </summary>
        /// <returns></returns>
        private async Task DoBackgroundWork() {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("images.mojaext");
            XElement xml;

            using (Stream stream = await file.OpenStreamForReadAsync()) {
                xml = XElement.Load(stream);
            }

            if (xml.Elements().Count() != 0) {
                 NotificationManager.NotifyUser();
            }
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();

            var tasks = BackgroundTaskRegistration.AllTasks;
            foreach (var task in tasks) {
               
                task.Value.Unregister(true);
            }
            //TODO: Save application state and stop any background activity
            if (Manager != null)
                await Manager.Save();
            var builder = new BackgroundTaskBuilder();
            builder.Name = "Background Time Trigger";
            builder.SetTrigger(new TimeTrigger(60, false)); 
            builder.Register();
            deferral.Complete();
        }

        /// <summary>
        /// Metoda koja se poziva kada se aplikacija pokrene dvoklikom na odgovarajuci fajl.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnFileActivated(FileActivatedEventArgs args) {

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null) {

                Manager = new JobManager();
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootFrame;
                if (rootFrame.Content == null) {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), args.Files);
                }

                Window.Current.Activate();
            }
        }
    }
}
