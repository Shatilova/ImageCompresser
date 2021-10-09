using ImageMagick;
using System;
using System.IO;
using System.Threading;

namespace ImageCompresser
{
    class Program
    {

        static int TotalFiles = 0;
        static int FilesDone = 0;

        static void WalkDirectoryTree(DirectoryInfo root)
        {
            foreach (var file in root.GetFiles("*"))
            {
                Thread.Sleep(1);
                FilesDone++;
            }

            foreach (var dir in root.GetDirectories())
            {
                WalkDirectoryTree(dir);
            }
        }

        static void Main(string[] args)
        {
            TotalFiles = Directory.GetFiles("E://Artem's", "*", SearchOption.AllDirectories).Length;
            /*int fCount = Directory.GetFiles("E://Artem's", "*.jpeg", SearchOption.AllDirectories).Length;

            using (MagickImage image = new("5peopleOutOf5years.png"))
            {
                image.Format = MagickFormat.Jpg;
                image.Quality = 75; // This is the Compression level.
                image.Write("YourFinalImage.jpg");
            }*/


            using (var progress = new ProgressBar())
            {
                new Thread(() => WalkDirectoryTree(new("E://Artem's"))).Start();

                while (FilesDone != TotalFiles)
                {
                    progress.Report(((double)FilesDone / TotalFiles));   
                }
            }
        }
    }
}
