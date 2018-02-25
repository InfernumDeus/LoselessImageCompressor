using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;

namespace LoselessImageCompressor
{
    class IsolatedStorageManager : IDisposable
    {
        private string filename;
        private IsolatedStorageFile isoStorage;
        
        public IsolatedStorageManager(IsolatedStorageFile IsolatedStorage, string filename)
        {
            // I have to set "en-US" as current culture 
            // to avoid crash during IsolatedStorageFileStream creation caused by Costura.Fody bug:
            // https://github.com/Fody/Costura/issues/138
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US", false);
            
            isoStorage = IsolatedStorage;
            this.filename = filename;
        }
        
        public void CreateTempFile(FileInfo sourceFile)
        {
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, isoStorage))
            using (FileStream file = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read))
            {
                file.CopyTo(isoStream);
            }
        }
        
        public void OverwriteByTempFile(FileInfo targetFile)
        {
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(filename, FileMode.Open, FileAccess.Read, isoStorage))
            using (FileStream file = new FileStream(targetFile.FullName, FileMode.Create, FileAccess.Write))
            {
                isoStream.CopyTo(file);
            }
        }

        public IsolatedStorageFileStream GetStream()
        {
            return new IsolatedStorageFileStream(filename, FileMode.Open, FileAccess.ReadWrite, isoStorage);
        }

        public decimal GetTempFileLenght()
        {
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(filename, FileMode.Open))
            {
                return isoStream.Length;
            }
        }

        public void Dispose()
        {
            isoStorage.Close();
        }
    }
}
