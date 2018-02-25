using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoselessImageCompressor
{
    static class FileHelper
    {
        private static List<string> systemFolders = new List<string> {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.Favorites),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            // Reduce "C:\Users\USERNAME\AppData\Local" to "C:\Users\USERNAME\AppData"
            string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Reverse().Skip(6).Reverse())
        };

        public static Queue<string> FindValidFiles(string root, Func<string, bool> isFileValid, Func<string, bool> isFolderValid)
        {
            if (root == null) { throw new ArgumentNullException("root"); }
            if (string.IsNullOrWhiteSpace(root)) { throw new ArgumentException("The passed value may not be empty or whithespace", "root"); }

            var files = new Queue<string>();

            var rootDirectory = new DirectoryInfo(root);
            if (rootDirectory.Exists == false) { return files; }

            root = rootDirectory.FullName;
            if (isFolderValid(root) == false) { return files; }

            var folders = new Queue<string>();
            folders.Enqueue(root);
            while (folders.Count != 0)
            {
                string currentFolder = folders.Dequeue();
                Console.WriteLine(currentFolder);

                try
                {
                    var currentFiles = Directory.EnumerateFiles(currentFolder, "*.*").Where(f => isFileValid(f));
                    foreach (string file in currentFiles)
                    {
                        files.Enqueue(file);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }

                try
                {
                    var currentSubFolders = Directory.GetDirectories(currentFolder).Where(f => isFolderValid(f));
                    foreach (string directory in currentSubFolders)
                    {
                        folders.Enqueue(directory);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }
            }
            return files;
        }

        public static bool IsFolderWithinSystemDirectory(string path)
        {
            if (systemFolders.Contains(path, StringComparer.OrdinalIgnoreCase)) return false;

            string parent = Directory.GetParent(path)?.FullName;
            while (parent != null)
            {
                if (systemFolders.Contains(parent, StringComparer.OrdinalIgnoreCase)) return false;

                parent = Directory.GetParent(parent)?.FullName;
            }
            return true;
        }
    }
}
