using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Zadatak2 {
    /// <summary>
    /// Stek DegreeOfParallelismPerImageControl komponenata
    /// </summary>
    public sealed partial class ImagesDialog : ContentDialog {
        public ImagesDialog() {
            this.InitializeComponent();
        }

        /// <summary>
        /// Obrada izabranih fajlova. Fajlovi se stavljaju na stackPanel i prikazuju korisniku
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task ProcessFiles(IReadOnlyList<StorageFile> files) {
            foreach (var file in files) {
                BitmapImage bitmapImage = await Utilities.GetBitmapImageFromFile(file);
                ImagesStackPanel.Children.Add(new DegreeOfParallelismPerImageControl(bitmapImage, file));
            }
        }

        /// <summary>
        /// Dobijanje relevantnih podataka o svakoj slici.
        /// </summary>
        /// <returns>Samu sliku, fajl u kome se nalazi i broj niti na kojima se moze izvrsavati</returns>
        public (BitmapImage image, StorageFile file, int numberOfCores)[] GetImagesWithNumberOfCores() => ImagesStackPanel.Children.OfType<DegreeOfParallelismPerImageControl>().Select(el => (el.ImageSource, el.File, el.NumOfThreads)).ToArray();

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }

    }
}
