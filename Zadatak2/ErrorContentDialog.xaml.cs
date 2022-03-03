using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Zadatak2 {

    /// <summary>
    /// ContentDialog za javljanje gresaka ili nedozvoljenih radnji korisniku
    /// </summary>
    public sealed partial class ErrorContentDialog : ContentDialog {
        public ErrorContentDialog(string errorTitle) {
            this.InitializeComponent();
            Title = errorTitle;
        }


        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }
    }
}
