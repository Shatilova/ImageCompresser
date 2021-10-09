using ImageMagick;
using System;
using System.Collections.Generic;
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

        static void WalkDirectoryTree(DirectoryInfo root)
        {
            foreach (var file in root.GetFiles("*"))
            {
                if (ImageExts.Contains(file.Extension))
                {
                    using (MagickImage image = new(file.FullName))
                    {
                        image.Format = MagickFormat.Jpg;
                        image.Quality = 75;
                        image.Write("YourFinalImage.jpg");
                    }
                } else
                {

                }

                //Thread.Sleep(1);
                FilesDone++;
            }

            foreach (var dir in root.GetDirectories())
            {
                if (dir.Name == "CompressedResult")
                    continue;

                WalkDirectoryTree(dir);
            }
        }

        static void Main(string[] args)
        {
            InPath = Directory.GetCurrentDirectory();
            OutPath = Directory.GetCurrentDirectory() + "\\CompressedResult";

            TotalFiles = Directory.GetFiles(InPath, "*", SearchOption.AllDirectories).Length;
            /*int fCount = Directory.GetFiles("E://Artem's", "*.jpeg", SearchOption.AllDirectories).Length;

            using (MagickImage image = new("5peopleOutOf5years.png"))
            {
                image.Format = MagickFormat.Jpg;
                image.Quality = 75; // This is the Compression level.
                image.Write("YourFinalImage.jpg");
            }*/


            using (var progress = new ProgressBar())
            {
                new Thread(() => WalkDirectoryTree(new(InPath))).Start();

                while (FilesDone != TotalFiles)
                {
                    progress.Report(((double)FilesDone / TotalFiles));   
                }
            }
        }
    }
}
