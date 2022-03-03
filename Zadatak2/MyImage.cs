using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Zadatak2 {
    public class MyImage {

        public BitmapImage Image { get; private set; }
        public StorageFile destinationFile;
        public StorageFile sourceFile;
        public int Height;
        public int Width;

        public int DegreeOfParallelism { get; private set; }
        public State CurrentState { get; private set; } = State.Pending;
        public bool IsFinished => CurrentState == State.Cancelled || CurrentState == State.Finished || CurrentState == State.Error;
        /// <summary>
        /// Trenutno stanje obrade slike.
        /// </summary>
        public enum State { Pending, Pausing, Paused, Resuming, Cancelling, Cancelled, Processing, Error, Finished };

        public delegate void ProgressReportedDelegate(double progress, State state);

        public event ProgressReportedDelegate ProgressChanged;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private Task imageProcessingTask;

        private readonly SemaphoreSlim pauseSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private readonly object locker = new object();

        public MyImage(BitmapImage image, StorageFile sourceFile, StorageFile destinationFile, int degreeOfParallelism) => (Image, this.sourceFile, this.destinationFile, DegreeOfParallelism) = (image, sourceFile, destinationFile, degreeOfParallelism);
        public MyImage(StorageFile sourceFile, StorageFile destinationFile, State state, int degreeOfParallelism) => (this.sourceFile, this.destinationFile, CurrentState, DegreeOfParallelism) = (sourceFile, destinationFile, state, degreeOfParallelism);
        public MyImage(StorageFile sourceFile, StorageFile destinationFile, int degreeOfParallelism) => (this.sourceFile, this.destinationFile, DegreeOfParallelism) = (sourceFile, destinationFile, degreeOfParallelism);


        /// <summary>
        /// Procesiranje slike, ako slika nije 24-bit .bmp formata, nece biti pravilno obradjena!
        /// </summary>
        /// <returns></returns>
        private async Task ProcessImage() {
            try {
                CurrentState = State.Processing;

                var handle = sourceFile.CreateSafeFileHandle(options: FileOptions.RandomAccess);
                using (var filein = new BinaryReader(new FileStream(handle, FileAccess.Read))) {

                    var Signature1 = filein.ReadByte(); // now at 0x1
                    var Signature2 = filein.ReadByte(); // now at 0x2
                    if (Signature1 != 66 || Signature2 != 77) // Must be BM
                    {
                        throw new ArgumentException("The file must be with a .bmp extension!");
                    }

                    int counter = 0;

                    filein.ReadDouble(); // skip next 8 bytes now at position      0xa    
                    var Offset = filein.ReadInt32(); // offset in file      now at 0ea               
                    filein.ReadInt32(); // now at 0x12a
                    Width = filein.ReadInt32(); // now at 0x16
                    Height = filein.ReadInt32(); // now at 0x1a


                    filein.ReadBytes(Offset - 0x1a);
                    byte[] srcPixels = filein.ReadBytes((int)filein.BaseStream.Length);

                    byte b, g, r;
                    byte[] destPixels = new byte[3 * Width * Height];


                    ParallelLoopResult parallelLoopResult = new ParallelLoopResult();

                    parallelLoopResult = Parallel.For(parallelLoopResult.LowestBreakIteration ?? 0, Height, new ParallelOptions { MaxDegreeOfParallelism = DegreeOfParallelism }, (y, state) => {


                        for (int x = 0; x < Width; x++) {

                            if (cancellationTokenSource.IsCancellationRequested) {
                                CurrentState = State.Cancelled;
                                ProgressChanged?.Invoke(0.0, CurrentState);
                                state.Stop();
                            }
                            else if (CurrentState == State.Pausing || CurrentState == State.Paused) {

                                CurrentState = State.Paused;
                                ProgressChanged?.Invoke((double)counter / Height, CurrentState);
                                pauseSemaphore.Wait();
                                pauseSemaphore.Release();
                            }
                            else if (CurrentState == State.Resuming) {
                                CurrentState = State.Processing;
                            }
                            b = srcPixels[(x + y * Width) * 3];
                            g = srcPixels[(x + y * Width) * 3 + 1];
                            r = srcPixels[(x + y * Width) * 3 + 2];

                            destPixels[(x + y * Width) * 3] = (byte)(Byte.MaxValue - b);     // B
                            destPixels[(x + y * Width) * 3 + 1] = (byte)(Byte.MaxValue - r); ; // G
                            destPixels[(x + y * Width) * 3 + 2] = (byte)(Byte.MaxValue - g); ; // R

                        }

                        lock (locker) {
                            counter++;
                            ProgressChanged?.Invoke((double)counter / Height, CurrentState);
                        }

                        //DA BI SE VIDIO NAPREDAK U PROGRESS BARU
                        Task.Delay(10).Wait();
                    }); //end of ParallelFor

                    if (parallelLoopResult.IsCompleted) {
                        CurrentState = State.Finished;
                    }


                    if (CurrentState == State.Cancelled)
                        return;


                    byte[] bmpBytes = Utilities.GetBMPBytesFromRawBytes(destPixels, Width, Height);

                    await FileIO.WriteBytesAsync(destinationFile, bmpBytes);

                    //kraj
                    ProgressChanged?.Invoke(1.0, CurrentState);
                }
            }
            catch (Exception e) {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            async () => {
                                ErrorContentDialog dialog = new ErrorContentDialog(e.Message);
                                await dialog.ShowAsync();
                            });

                CurrentState = State.Error;
                ProgressChanged?.Invoke(0, CurrentState);
            }

        }


        public async Task Start() {
            await semaphore.WaitAsync();
            try {
                if (CurrentState == State.Pending) {
                    imageProcessingTask = Task.Factory.StartNew(async () => await ProcessImage(), cancellationTokenSource.Token);
                }
                else
                    throw new InvalidOperationException("Task has already been started!");
            }
            finally {
                semaphore.Release();
            }
        }

        public async Task Cancel() {
            await semaphore.WaitAsync();
            try {
                if (CurrentState == State.Pending)
                    CurrentState = State.Cancelled;
                else if (CurrentState == State.Processing || CurrentState == State.Resuming || CurrentState == State.Pausing || CurrentState == State.Paused) {
                    CurrentState = State.Cancelling;
                    cancellationTokenSource.Cancel();
                }
                else
                    throw new InvalidOperationException("The task cannot be cancelled because it was previously completed!");
            }
            finally {
                semaphore.Release();
            }
        }

        public async Task Pause() {
            await semaphore.WaitAsync();
            try {
                if (CurrentState == State.Processing) {
                    CurrentState = State.Pausing;
                    await pauseSemaphore.WaitAsync();

                }
                else
                    throw new InvalidOperationException("The task cannot be paused!");
            }
            finally {
                semaphore.Release();
            }
        }

        public async Task Resume() {
            await semaphore.WaitAsync();
            try {
                if (CurrentState == State.Paused || CurrentState == State.Pausing) {
                    CurrentState = State.Resuming;
                    pauseSemaphore.Release();
                }
                else
                    throw new InvalidOperationException("The task cannot be resumed!");
            }
            finally {
                semaphore.Release();
            }
        }

        public override bool Equals(object obj) {
            var item = obj as MyImage;

            if (item == null) {
                return false;
            }

            return this.sourceFile.Path.Equals(item.sourceFile.Path) && this.destinationFile.Path.Equals(item.destinationFile.Path);
        }


        /// <summary>
        /// Dohvatanje parametara potrebnih za serijalizaciju, a kasniju deserijalizaciju slike slike.
        /// </summary>
        /// <returns></returns>
        public (StorageFile sourceFile, StorageFile destinationFile, int DegreeOfParallelism) GetSerializationParameters() => (sourceFile, destinationFile, DegreeOfParallelism);


    }
}
