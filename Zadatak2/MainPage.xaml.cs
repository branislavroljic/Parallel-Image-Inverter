using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


using static Windows.Storage.AccessCache.StorageApplicationPermissions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Zadatak2 {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {

        public MainPage() {
            this.InitializeComponent();
            jobManager = (Application.Current as App).Manager;
        }


        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            try {
                base.OnNavigatedTo(e);
                if (e.Parameter is IReadOnlyList<IStorageItem>) {
                    IStorageFile file = (IStorageFile)(((IReadOnlyList<IStorageItem>)e.Parameter)[0]);

                    XElement xml;
                    using (Stream stream = await file.OpenStreamForReadAsync())
                        xml = XElement.Load(stream);


                    List<MyImage> images = new List<MyImage>();
                    foreach (var element in xml.Elements()) {
                        StorageFile sourceFile = await FutureAccessList.GetFileAsync(element.Attribute("sourceFile").Value);
                        StorageFile destinationFile = await FutureAccessList.GetFileAsync(element.Attribute("destinationFile").Value);

                        int degreeOfParallelism = Convert.ToInt32(element.Attribute("degreeOfParallelism").Value);
                        MyImage tempImage = new MyImage(sourceFile, destinationFile, degreeOfParallelism);
                        if (!images.Contains(tempImage))
                            images.Add(tempImage);
                    }
                    jobManager.AddInitializedImages(images);

                    await InitializeStackPanel(jobManager.Images);

                }
                else {
                    await InitializeStackPanel(jobManager.Images);
                }
            }
            catch (Exception ed) {
                ErrorContentDialog dialog = new ErrorContentDialog(ed.Message);
                await dialog.ShowAsync();
            }
        }


        readonly JobManager jobManager;

        /// <summary>
        /// Handler kada korisnik stisne dugme za izbor slika. Bira slike, zatim specifikuje broj niti za svaku, a zatim bira i folder gdje ce odabrane slike
        /// biti sacuvane. Podrazumijevano je moguce biranje slika sa .bmp ekstenzijom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Choose_Images_Button_Click(object sender, RoutedEventArgs e) {

            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".bmp");

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();

            if (files.Count > 0) {

                ImagesDialog dialog = new ImagesDialog();
                await dialog.ProcessFiles(files);
                ContentDialogResult dialogResult = await dialog.ShowAsync();

                if (dialogResult == ContentDialogResult.Primary) {


                    FolderPicker folderPicker = new FolderPicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
                    folderPicker.FileTypeFilter.Add("*");

                    StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                    if (folder != null) {


                        FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                        MyImage[] images = await jobManager.AddImages(dialog.GetImagesWithNumberOfCores(), folder);

                        await InitializeStackPanel(images);

                    }

                }
            }
        }


        //Handleri za odgovarajuce evente.
        private async void ImageProgressControl_JobCancelled(MyImage image, object sender) => await image.Cancel();
        private async void ImageProgressControl_JobPaused(MyImage image, object sender) => await image.Pause();
        private async void ImageProgressControl_JobResumed(MyImage image, object sender) => await image.Resume();

        /// <summary>
        /// Prikaz odabranih slika korinsiku uz mogucnost da odabere broj niti na kojima ce se izvrsavati obrada svake slike.
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        private async Task InitializeStackPanel(IReadOnlyList<MyImage> images) {

            foreach (MyImage image in images)
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {

                    ImageProgressControl imageProgressControl = new ImageProgressControl(image);
                    if (!imageProgressControl.isSourceInitialized) {
                        BitmapImage imageSource = await Utilities.GetBitmapImageFromFile(image.sourceFile);
                        imageProgressControl.SetImageProgressControlImage(imageSource);
                    }
                    imageProgressControl.JobPaused += ImageProgressControl_JobPaused;
                    imageProgressControl.JobCancelled += ImageProgressControl_JobCancelled;
                    imageProgressControl.JobResumed += ImageProgressControl_JobResumed;
                    ImagesStackPanel.Children.Add(imageProgressControl);
                });
        }


        /// <summary>
        /// Ukoliko korisnik stisne start, a nema slika koje cekaju na izvrsavanje ili su sve 'jezgre' zauzete, pokretanje nije moguce!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StartButton_Click(object sender, RoutedEventArgs e) {

            if (jobManager.Images.Where(i => i.CurrentState == MyImage.State.Pending).Count() == 0) {
                ErrorContentDialog dialog = new ErrorContentDialog("You have to select some images to process them!");
                await dialog.ShowAsync();
                return;
            }
            if (jobManager.Images.Where(i => !i.IsFinished && !(i.CurrentState == MyImage.State.Pending)).Count() == jobManager.MaxParallelJobs) {
                ErrorContentDialog dialog = new ErrorContentDialog("All threads are busy!");
                await dialog.ShowAsync();
                return;
            }
            await jobManager.ProcessImages();
            MaxParallelJobsTextBox.Text = "Number of cores: " + jobManager.MaxParallelJobs;
            MaxParallelJobsTextBox.IsReadOnly = true;
            SubmitButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Button koji korisnik stisne kada unese zeljeni broj jezgara.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SumbitButton_Click(object sender, RoutedEventArgs e) {
            try {
                int number = Convert.ToInt32(MaxParallelJobsTextBox.Text);
                if (number > 10) {
                    ErrorContentDialog dialog = new ErrorContentDialog("The number of parallel tasks cannot exceed 10!");
                    await dialog.ShowAsync();
                }
                else {
                    jobManager.MaxParallelJobs = number;
                }
            }
            catch {
                return;
            }
        }

    }

}
