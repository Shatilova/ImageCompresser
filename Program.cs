using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ImageCompresser
{
    class Program
    {
        static List<string> ImageExts = new() { ".jpg", ".jpeg", ".png" };
        static string InPath;
        public static string OutPath;
        static string ProgramPath;

        static int TotalFiles = 0;
        static int FilesDone = 0;
        static int ImagesDone = 0;
        static int SmallImagesCount = 0;
        static int ErrorsCount = 0;
        static int NotImages = 0;
                
        static Stopwatch sw;
        static string Status = "";

        static void ProcessDirectory(DirectoryInfo root)
        {
            StringBuilder log = new();
            try
            {
                log.AppendLine($"Root - {root.FullName}");
                string relative = root.FullName.Replace(InPath, "");
                log.AppendLine($"RelativePath - {relative}");
                Directory.CreateDirectory(OutPath + relative);
                log.AppendLine($"OutPath created - {OutPath + relative}");

                foreach (var file in root.GetFiles("*"))
                {
                    string output = $"{OutPath}{relative}\\{file.Name}";
                    log.AppendLine($"OutputFile - {$"{OutPath}{relative}\\{file.Name}"}");
                    if (ImageExts.Contains(file.Extension.ToLower()))
                    {
                        if (file.Length > (1024 * 1024))
                        {
                            log.AppendLine($"File - {file.FullName}");
                            Status = $"Сжатие ..{file.FullName.Substring(InPath.Length)}";
                            log.AppendLine($"Status - {Status}");
                            try
                            {
                                using (MagickImage image = new(file.FullName))
                                {
                                    image.Format = MagickFormat.Jpg;
                                    image.Quality = 75;
                                    image.Write($"{OutPath}{relative}\\{file.Name}");
                                }
                                log.AppendLine($"OutFile created!");
                                ImagesDone++;
                            }
                            catch
                            {
                                ErrorsCount++;
                            }
                        }
                        else
                        {
                            Status = $"Пропуск ..{file.FullName.Replace(root.FullName, "")}";
                            File.Copy(file.FullName, output);
                            SmallImagesCount++;
                        }                                          
                    }
                    else
                    {
                        Status = $"Копирование ..{file.FullName.Replace(root.FullName, "")}";
                        File.Copy(file.FullName, output);
                        NotImages++;
                    }

                    FilesDone++;
                }

                foreach (var dir in root.GetDirectories())
                {
                    if (dir.Name == "CompressedResult")
                        continue;

                    ProcessDirectory(dir);
                }
            } catch (Exception ex)
            {
                File.WriteAllText(ProgramPath + "\\error.log", log.ToString() + "\nException:\n" + ex.Message);
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
            ProgramPath = Directory.GetCurrentDirectory();

            Console.Write("Введите путь:\n> ");
            InPath = Console.ReadLine();
            if (!Directory.Exists(InPath))
            {
                Console.WriteLine("Директория не существует");
                Console.ReadKey();
                return;
            }

            
            OutPath = InPath + "\\CompressedResult";            
            if(Directory.Exists(OutPath))
            {
#if DEBUG
                DeleteDirectory(OutPath);
#else
                Console.WriteLine("Обнаружена папка \"CompressedResult\". Там хранятся результаты прошлого сжатия. Удалите её, если хотите запутить процесс сжатия, и перезапустите программу");
                Console.ReadKey();
                return;
#endif
            }

            TotalFiles = Directory.GetFiles(InPath, "*", SearchOption.AllDirectories).Length;
            using (var progress = new ProgressBar())
            {
                sw = Stopwatch.StartNew();
                new Thread(() => ProcessDirectory(new(InPath))).Start();

                int oldFilesDone = 0;
                while (FilesDone != TotalFiles)
                {
                    oldFilesDone = FilesDone;

                    progress.Report(((double)FilesDone / TotalFiles));
                    if (FilesDone > 0)
                        progress.ReportInfo(Status, TimeSpan.FromMilliseconds((int)((sw.ElapsedMilliseconds * (TotalFiles - FilesDone)) / FilesDone)));

                    while (FilesDone == oldFilesDone)
                        Thread.Sleep(20);
                }
            }

            sw.Stop();

            Console.Clear();
            StringBuilder resLog = new();
            resLog.AppendLine($"Сжато изображений - {ImagesDone} ({DirectoryExt.GetSizeInMegabytes(InPath, ImageExts.ToArray(), minSize: (1024 * 1024))} Мб >>> {DirectoryExt.GetSizeInMegabytes(OutPath, ImageExts.ToArray(), minSize: (1024 * 1024))} Мб)");

            if (SmallImagesCount > 0)
                resLog.AppendLine($"Пропущено изображений меньше 1Мб - {SmallImagesCount} ({DirectoryExt.GetSizeInMegabytes(InPath, ImageExts.ToArray(), maxSize: (1024 * 1024))} Мб)");

            if (NotImages > 0)
                resLog.AppendLine($"Перемещено файлов - {NotImages} ({DirectoryExt.GetSizeInMegabytes(InPath, excludeExts: ImageExts.ToArray())} Мб)");

            if (ErrorsCount > 0)
                resLog.AppendLine($"Битых изображений - {ErrorsCount}");

            resLog.AppendLine($"Всего файлов - {TotalFiles} ({DirectoryExt.GetSizeInMegabytes(InPath)} Мб >>> {DirectoryExt.GetSizeInMegabytes(OutPath)} Мб)")
                .AppendLine($"Затрачено времени - {sw.Elapsed.Hours.ToString().PadLeft(2, '0')}:{sw.Elapsed.Minutes.ToString().PadLeft(2, '0')}:{sw.Elapsed.Seconds.ToString().PadLeft(2, '0')}");

            File.WriteAllText(ProgramPath +"\\result.log", new StringBuilder().AppendLine($"Время начала операции - {DateTime.Now.AddMilliseconds(-sw.ElapsedMilliseconds)}").ToString() + resLog.ToString());
            Console.WriteLine(resLog.ToString() + "\nДанная информация продублирована в файл \"result.log\"");
            Console.ReadKey();
        }
    }

    public static class DirectoryExt
    {
        public static long GetSizeInMegabytes(string path, string[] includeExts = null, string[] excludeExts = null, long minSize = 0, long maxSize = long.MaxValue)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            var files = dir.EnumerateFiles("*", SearchOption.AllDirectories).Where(e => e.Length > minSize && e.Length < maxSize);
            if (!path.StartsWith(Program.OutPath))
                files = files.Where(e => !e.FullName.StartsWith(Program.OutPath));

            if (includeExts == null)
            {
                if (excludeExts == null)
                {
                    return files.Sum(e => e.Length) / (1024 * 1024);
                }
                else
                {
                    return files
                        .Where(e => !excludeExts.Any(f => e.Name.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
                        .Sum(e => e.Length) / (1024 * 1024);
                }
            }
            else
            {
                if (excludeExts == null)
                {
                    return files
                        .Where(e => includeExts.Any(f => e.Name.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
                        .Sum(e => e.Length) / (1024 * 1024);
                }
                else
                {
                    return files
                    .Where(e => includeExts.Any(f => e.Name.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
                    .Sum(e => e.Length) / (1024 * 1024);
                }
            }
        }
    }
}
