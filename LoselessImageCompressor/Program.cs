using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ImageMagick;
using System.IO.IsolatedStorage;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace LoselessImageCompressor
{
    class Program
    {
        private static ImageOptimizer optimizer;
        private static object consoleLocker = new object();

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine(
@"Loseless Image Compressor 1.0 / 24 February 2018
Author: Rustam Khuzin
E-mail: infernumdeus@mail.ru
GitHub: github.com/InfernumDeus
Description: this program tries to find every image in selected folders (and
             subfolders) and to compress it without losing any quality by
             using Magic.NET. Usually you can get around 10%-20% compression
             this way for photos.

             If something will go wrong your files will be left unchanged.
             Also it completely ignores everything in system folders like 
             ""Windows"", ""Program Files"", ""ProgramData"" and ""AppData"".
             But I still suggest you to don't pick folders that contain some
             programs within.

             If you would like it to work faster try to close some other
             applications. Especially heavy ones like games and browsers.
------------------------------------------------------------------------------
Please select directory"
            );

            string startTime = DateTime.Now.ToString("HH:mm:ss tt");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            optimizer = new ImageOptimizer();

            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.Cancel) return;

            Console.WriteLine("Searching images");
            Queue<string> filesQueue = null;
            filesQueue = FileHelper.FindValidFiles(fbd.SelectedPath, 
                                                   optimizer.IsSupported, 
                                                   FileHelper.IsFolderWithinSystemDirectory);

            if (filesQueue.Count < 1)
            {
                Console.WriteLine("No images found \r\nPress Enter to close");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("------------------------------------------------------------------------------");
            Console.WriteLine("Started compressing");

            int CPUCores = Environment.ProcessorCount;
            Task<bool>[] taskArray = new Task<bool>[CPUCores];

            int filesCounter = 0;
            decimal before = 0;
            decimal after = 0;
            for (int i = 0; i < CPUCores; i++)
            {
                taskArray[i] = Task<bool>.Factory.StartNew(() => CompressFiles(filesQueue, filesQueue.Count, ref filesCounter, ref before, ref after));
            }

            Task.WaitAll(taskArray);

            bool successfullyCompleted = true;
            for (int i = 0; i < CPUCores; i++)
            {
                if (taskArray[i].Result == false) successfullyCompleted = false;
            }
            
            Console.WriteLine("Started at: {0}", startTime);

            if (successfullyCompleted)
            {
                Console.WriteLine("Completed at: {0}", DateTime.Now.ToString("HH:mm:ss tt"));
            }
            else
            {
                Console.WriteLine("Completed with error at: {0}", DateTime.Now.ToString("HH:mm:ss tt"));
            }

            Console.WriteLine("Original size of images: {0}", ShortenBytesNumber(before));
            Console.WriteLine("Resulting size of images: {0}", ShortenBytesNumber(after));
            Console.WriteLine("Compression: {0} ({1})",
                              ShortenBytesNumber(before - after),
                              ((before - after) / before).ToString("P", System.Globalization.CultureInfo.InvariantCulture));

            Console.WriteLine("Press Enter to close");
            Console.ReadLine();
        }

        // Returns false if catched critical exception
        private static bool CompressFiles(Queue<string> files, int numberOfFiles, ref int filesCounter, ref decimal before, ref decimal after)
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            using (var isoStoreManager = new IsolatedStorageManager(IsolatedStorageFile.GetUserStoreForDomain(), Task.CurrentId + "tempfile.dat"))
            {
                FileInfo originalFile = null;
                
                while (files.Count > 0)
                {
                    string filepath = files.Dequeue();

                    lock (consoleLocker)
                    {
                        filesCounter++;
                        Console.WriteLine("({0}/{1}) {2}", filesCounter, numberOfFiles, filepath);
                    }

                    try
                    {
                        originalFile = new FileInfo(filepath);
                        if (!originalFile.Exists) continue;
                        if (originalFile.IsReadOnly) continue;

                        decimal originalFileSize = originalFile.Length;
                        before += originalFileSize;

                        DateTime[] timestamps = new DateTime[3];
                        timestamps[0] = originalFile.CreationTime;
                        timestamps[1] = originalFile.LastWriteTime;
                        timestamps[2] = originalFile.LastAccessTime;
                        
                        isoStoreManager.CreateTempFile(originalFile);

                        bool test;
                        using (IsolatedStorageFileStream stream = isoStoreManager.GetStream())
                        {
                            test = optimizer.LosslessCompress(stream);
                        }

                        if (originalFileSize > isoStoreManager.GetTempFileLenght())
                        {
                            isoStoreManager.OverwriteByTempFile(originalFile);

                            originalFile.CreationTime = timestamps[0];
                            originalFile.LastWriteTime = timestamps[1];
                            originalFile.LastAccessTime = timestamps[2];

                            originalFile.Refresh();
                        }
                    }
                    catch (Exception ex) when (ex is MagickException || ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                    {
                        lock (Logger.synchronizationObject)
                        {
                            Logger.LogSkippedFile(ex.GetType().Name, ex.Message, filepath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Clear queue to stop other tasks
                        files.Clear();

                        Console.WriteLine("Unexpected error: " + ex.GetType().Name);
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Occured when working with file: " + filepath);
                        Console.WriteLine("Press Enter to close");
                        Console.ReadLine();
                        return false;
                    }
                    finally
                    {
                        after += originalFile.Length;
                    }
                }
            }         
            return true;
        }

        private static string ShortenBytesNumber(decimal bytesNumber)
        {
            string[] sizes = { "B", "Kb", "Mb", "Gb", "Tb" };
            int order = 0;

            while (bytesNumber >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytesNumber = bytesNumber / 1024;
            }

            return String.Format("{0:0.##} {1}", bytesNumber, sizes[order]);
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine("Unhandled exception: " + e.ExceptionObject); 
            Console.WriteLine("Press Enter to close");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}
