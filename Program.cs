using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ImageCompresser
{
    class Program
    {
        static int TotalFiles = 0;
        static int FilesDone = 0;
        static List<string> ImageExts = new() { ".jpg", ".jpeg", ".png" };

        static string InPath = "Raw";
        static string OutPath = "CompressedResult";

        static Stopwatch sw;

        static void WalkDirectoryTree(DirectoryInfo root)
        {
            string relative = root.FullName.Replace(InPath, "");
            Directory.CreateDirectory(OutPath + relative);
            foreach (var file in root.GetFiles("*"))
            {
                string output = $"{OutPath}{relative}\\{file.Name}";
                if (ImageExts.Contains(file.Extension))
                {
                    using (MagickImage image = new(file.FullName))
                    {
                        image.Format = MagickFormat.Jpg;
                        image.Quality = 75;
                        image.Write($"{OutPath}{relative}\\{file.Name}");
                    }
                } else
                {
                    File.Copy(file.FullName, output);
                }

                FilesDone++;
            }

            foreach (var dir in root.GetDirectories())
            {
                if (dir.Name == "CompressedResult")
                    continue;

                WalkDirectoryTree(dir);
            }
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
        static void Main(string[] args)
        {
            InPath = Directory.GetCurrentDirectory();
            OutPath = Directory.GetCurrentDirectory() + "\\CompressedResult";
            
            if(Directory.Exists(OutPath))
            {
#if DEBUG
                DeleteDirectory(OutPath);
#else
                Console.WriteLine("Обнаружена папка \"CompressedResult\". Там хранятся результаты прошлого конвертирования. Удалите её, если хотите запутить процесс сжатия, и перезапустите программу");
                Console.ReadKey();
                return;
#endif
            }

            TotalFiles = Directory.GetFiles(InPath, "*", SearchOption.AllDirectories).Length;
            using (var progress = new ProgressBar())
            {
                new Thread(() => WalkDirectoryTree(new(InPath))).Start();

                while (FilesDone != TotalFiles)
                {
                    progress.Report(((double)FilesDone / TotalFiles));
                    Thread.Sleep(20);
                }
            }
        }
    }
}
