using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Zadatak2 {
    /// <summary>
    /// Komponenta koja sadrzi sliku koja se procesira, progress bar, stanje slike i Button-e za zaustavljanje, pokretanje i otkazivanje taska
    /// </summary>
    public sealed partial class ImageProgressControl : UserControl {
        public ImageProgressControl(MyImage image) {
            this.InitializeComponent();
            Image = image;
            ImageInProgressControl.Source = image.Image;
            UpdateButtonVisibility();
            image.ProgressChanged += Image_ProgressChanged;
            ImageProgressBar.Value = 0.0;
            ImageProgressBar.Maximum = 1.0;
        }
        public bool isSourceInitialized => ImageInProgressControl.Source != null;
        public void SetImageProgressControlImage(BitmapImage image) {
            ImageInProgressControl.Source = image;
        }

        public delegate void ImageActionCompletedDelegate(MyImage image, object sender);

        public MyImage Image { get; private set; }

        public event ImageActionCompletedDelegate JobCancelled;
        public event ImageActionCompletedDelegate JobPaused;
        public event ImageActionCompletedDelegate JobResumed;
        public event ImageActionCompletedDelegate JobCompleted;

        /// <summary>
        /// Mijanjanje vidljivosti Button - a u zavisnosti od stanja obrade u kome se nalazi slika
        /// </summary>
        private void UpdateButtonVisibility() {

            ImageProgressBar.Visibility = CancelButton.Visibility = PauseButton.Visibility = (!(Image.IsFinished || Image.CurrentState == MyImage.State.Pending)) ? Visibility.Visible : Visibility.Collapsed;

            CancelButton.IsEnabled = Image.CurrentState != MyImage.State.Cancelling && Image.CurrentState != MyImage.State.Cancelled && Image.CurrentState != MyImage.State.Pausing && Image.CurrentState != MyImage.State.Paused;

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => JobCancelled?.Invoke(Image, sender);

        /// <summary>
        /// Obrada pause i play buttona. Predstavljeni su kao jedan button, a pri pritisku, mijenja se i ikonica na buttonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseButton_Click(object sender, RoutedEventArgs e) {
            if (PausePlayIcon.Symbol.Equals(Symbol.Play)) {
                JobResumed?.Invoke(Image, sender);
                PausePlayIcon.Symbol = Symbol.Pause;

                if (CancelButton.Visibility == Visibility.Collapsed) {
                    CancelButton.Visibility = Visibility.Visible;
                }

            }
            else {
                JobPaused?.Invoke(Image, sender);
                PausePlayIcon.Symbol = Symbol.Play;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Progress bar za kontrolu napretka!
        /// </summary>
        /// <param name="progress"> kolicina obradjenosti, realni broj od 0.0 do 1.0</param>
        /// <param name="state">stanje u kome se slika nalazi</param>
        private async void Image_ProgressChanged(double progress, MyImage.State state) {

            //rasporedjivanje posla na UI thread, jer se ova fja poziva iz worker threada
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                if (!double.IsNaN(progress))
                    ImageProgressBar.Value = progress;

                ImageProgressInfoTextBlock.Text = state.ToString() + (state.ToString().EndsWith("ing") ? "....." : ".");

                UpdateButtonVisibility();

                if (state == MyImage.State.Finished)
                    JobCompleted?.Invoke(Image, this);
            });
        }

        /// <summary>
        /// Button koji ima za cilj cuvanje neobradjene slike, ako korisnik stisne button, moci ce sacuvati 'raw' sliku na proizvoljnu lokaciju, tako da ce 
        /// dvoklikom na taj fajl moci pokrenuti obradu slike ponovo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void On_Save_Clicked(object sender, RoutedEventArgs e) {

            SaveDialog dialog = new SaveDialog(Image);

            ContentDialogResult dialogResult = await dialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary) {
                await dialog.SaveRawImageAsync();
            }
        }


    }
}
