
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

using static Windows.Storage.AccessCache.StorageApplicationPermissions;

namespace Zadatak2 {
    public class JobManager {

        //maksimalan broj slika koje se mogu paralelno obradjivati
        private static readonly int maxConcurrentJobs = Environment.ProcessorCount;

        //maksimalan broj niti na kojima se jedna slika moze izvrsavati
        private static readonly int maxDegreeOfParallelism = Environment.ProcessorCount;

        private readonly string negativeFileName = "_negative";

        private readonly List<MyImage> images;

        public IReadOnlyList<MyImage> Images => images;

        public StorageFolder DestinationFolder { get; set; }

        public int MaxParallelJobs { get; set; } = maxConcurrentJobs;

        public JobManager(List<MyImage> images) => this.images = images;


        public JobManager() : this(new List<MyImage>()) { }

        /// <summary>
        /// Dodavanje slika u red cekanja za obradu.
        /// </summary>
        /// <param name="list"></param>
        public void AddInitializedImages(List<MyImage> list) {
            foreach (var myImage in list) {

                myImage.ProgressChanged += MyImage_ProgressChanged;

                this.images.Add(myImage);
            }
        }

        /// <summary>
        /// Dodavanje slika u red cekanja za obradu. Ako je slika vec obradjivana od pocetka pokretanja aplikacije(slika je obradjivana ako ima isti sourceFile i destinationFile)
        /// </summary>
        /// <param name="imagesInfo"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task<MyImage[]> AddImages((BitmapImage image, StorageFile sourceFile, int degreeOfParallelism)[] imagesInfo, StorageFolder folder) {

            List<MyImage> images = new List<MyImage>();

            DestinationFolder = folder;

            foreach ((BitmapImage image, StorageFile sourceFile, int degreeOfParallelism) in imagesInfo) {


                string fileName = Path.GetFileNameWithoutExtension(sourceFile.Name);
                StorageFile destinationFile = await folder.CreateFileAsync(fileName + negativeFileName + ".bmp", CreationCollisionOption.ReplaceExisting);
                MyImage myImage = new MyImage(image, sourceFile, destinationFile, degreeOfParallelism > 4 ? maxDegreeOfParallelism : degreeOfParallelism);

                myImage.ProgressChanged += MyImage_ProgressChanged;

                if (!this.images.Contains(myImage)) {
                    this.images.Add(myImage);
                    images.Add(myImage);
                }
                else {
                    ErrorContentDialog dialog = new ErrorContentDialog("The selected image will not be added because it already exists in the application!");
                    await dialog.ShowAsync();
                }
            }
            return images.ToArray();
        }


        private async void MyImage_ProgressChanged(double progress, MyImage.State state) {
            if (state == MyImage.State.Finished || state == MyImage.State.Error || state == MyImage.State.Cancelled) {
                await ProcessImages();
            }
        }

        /// <summary>
        /// Procesitanje slika. Slike sa stanjem obrade == Pending ce biti obradjene samo ako nije vec iskoristen dozvoljeni broj jezgara
        /// </summary>
        /// <returns></returns>
        public async Task ProcessImages() {
            //nadji sve slike koje se obradjuju
            int currentParallelJobs = images.Count(image => image.CurrentState == MyImage.State.Processing || image.CurrentState == MyImage.State.Pausing || image.CurrentState == MyImage.State.Paused || image.CurrentState == MyImage.State.Resuming);
            if (currentParallelJobs < MaxParallelJobs) {
                MyImage[] pendingImages = images.Where(image => image.CurrentState == MyImage.State.Pending).Take(MaxParallelJobs - currentParallelJobs).ToArray();

                foreach (var image in pendingImages) {
                    await image.Start();
                }
            }
        }

        //kreiranje serijalizacionong fajla
        private static async Task<StorageFile> CreateSerializationFile() => await ApplicationData.Current.LocalFolder.CreateFileAsync("images.mojaext", CreationCollisionOption.ReplaceExisting);

        //dohvatanje serijalizaciono fajla
        public static async Task<StorageFile> GetSerializationFile() => await ApplicationData.Current.LocalFolder.CreateFileAsync("images.mojaext", CreationCollisionOption.OpenIfExists);

        /// <summary>
        /// Serijalizacija slika tako da se obezbijedi perzistencija posla.
        /// </summary>
        /// <returns></returns>
        public async Task Save() {
            try {

                XElement xml = new XElement(nameof(JobManager), images.Distinct().Where(i => i.CurrentState != MyImage.State.Error && i.CurrentState != MyImage.State.Finished && i.CurrentState != MyImage.State.Cancelled)
                                                                                              .Select(x => x.GetSerializationParameters()).Select(x => new XElement(nameof(MyImage), new XAttribute("sourceFile", FutureAccessList.Add(x.sourceFile)),
                                                                                                                                                        new XAttribute("destinationFile", FutureAccessList.Add(x.destinationFile)),
                                                                                                                                                        new XAttribute("degreeOfParallelism", x.DegreeOfParallelism))));

                StorageFile file = await CreateSerializationFile();

                using (Stream stream = await file.OpenStreamForWriteAsync())
                    xml.Save(stream);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Ucitavanje slika koje su prethodno serijalizovane.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<JobManager> Load(IStorageFile file) {
            try {

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
                    else {
                        ErrorContentDialog dialog = new ErrorContentDialog("The selected image will not be added because it already exists in the application!");
                        await dialog.ShowAsync();
                    }

                }

                return new JobManager(images);
            }
            catch {

                return new JobManager();
            }
        }


    }
}
