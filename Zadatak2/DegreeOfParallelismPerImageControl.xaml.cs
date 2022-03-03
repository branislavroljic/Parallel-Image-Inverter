using System;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Zadatak2 {
    // Komponenta koja se koristi pri inicijalizaciji slika sa brojem niti na kojima ce se izvrsavati

    public sealed partial class DegreeOfParallelismPerImageControl : UserControl {
        //ako korisnik ne unese nista, podrazumijevano je izvrsavanje na jednoj niti
        static readonly int defaultNumOfCores = 1;
        public DegreeOfParallelismPerImageControl(BitmapImage bitmapImage, StorageFile file) {

            this.InitializeComponent();
            ImageForProcess.Source = bitmapImage;
            ImageSource = ImageForProcess.Source as BitmapImage;
            NumOfThreads = defaultNumOfCores;
            CoresTextBox.Text = defaultNumOfCores.ToString();
            File = file;
        }

        public StorageFile File { get; private set; }
        public int NumOfThreads { get; private set; }

        public BitmapImage ImageSource { get; private set; }

        //obrada korisnickog unosa broja niti
        private void NumOfCores_TextChanged(object sender, TextChangedEventArgs e) {
            try {
                NumOfThreads = Convert.ToInt32(((TextBox)sender).Text);
            }
            catch {
                return;
            }
        }
    }
}
