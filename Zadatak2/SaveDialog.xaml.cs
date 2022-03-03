using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;


using static Windows.Storage.AccessCache.StorageApplicationPermissions;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Zadatak2 {
    public sealed partial class SaveDialog : ContentDialog {
        public SaveDialog(MyImage image) {
            this.InitializeComponent();
            Image = image;
        }

        private MyImage Image { get; set; }
        /// <summary>
        /// Metoda se koristi ako korisnik stisne Button i odobri cuvanje 'raw' slike. Pravi se fajl cija ekstenzija je registrovana tako da ce dvoklikom na isti fajl 
        /// aplikacija biti pokrenuta i slika ce zapoceti obradu.
        /// </summary>
        /// <returns></returns>
        public async Task SaveRawImageAsync() {
            FolderPicker folderPicker = new FolderPicker() { SuggestedStartLocation = PickerLocationId.Desktop };
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
           
            if (folder != null) {
                FutureAccessList.Add(folder);
                try {
                    var imageInfo = Image.GetSerializationParameters();
                    XElement xml = new XElement(nameof(JobManager), new XElement(nameof(MyImage), new XAttribute("sourceFile", FutureAccessList.Add(imageInfo.sourceFile)),
                                                                                                                                                        new XAttribute("destinationFile", FutureAccessList.Add(imageInfo.destinationFile)),
                                                                                                                                                       new XAttribute("degreeOfParallelism", imageInfo.DegreeOfParallelism)));
                    StorageFile file = await folder.CreateFileAsync(Path.GetFileNameWithoutExtension(imageInfo.sourceFile.Name) + ".mojaext", CreationCollisionOption.ReplaceExisting);

                    using (Stream stream = await file.OpenStreamForWriteAsync())
                        xml.Save(stream);

                }
                catch {
                }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }
    }
}
