using System;
using System.Threading;

// taken from: https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
namespace ImageCompresser
{
	/// <summary>
	/// An ASCII progress bar
	/// </summary>
	public class ProgressBar : IDisposable, IProgress<double>
	{
		private const int blockCount = 50;
		private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private const string animation = @"|/-\";

		private readonly Timer timer;

		private double currentProgress = 0;
		private string currentStatus = "1";
		private string leftTime = "";
		private bool disposed = false;
		private int animationIndex = 0;

		public ProgressBar()
		{
			timer = new Timer(TimerHandler);

			// A progress bar is only for temporary display in a console window.
			// If the console output is redirected to a file, draw nothing.
			// Otherwise, we'll end up with a lot of garbage in the target file.
			if (!Console.IsOutputRedirected)
			{
				ResetTimer();
			}
		}

		public void Report(double value)
		{
			// Make sure value is in [0..1] range
			value = Math.Max(0, Math.Min(1, value));
			Interlocked.Exchange(ref currentProgress, value);
		}

		public void ReportInfo(string status, TimeSpan timeLeft)
		{
			Interlocked.Exchange(ref currentStatus, status);
			var newTimeLeft = new DateTime(2021, 1, 1, timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds);
			Interlocked.Exchange(ref leftTime, $"{timeLeft.Hours.ToString().PadLeft(2, '0')}:{timeLeft.Minutes.ToString().PadLeft(2, '0')}:{timeLeft.Seconds.ToString().PadLeft(2, '0')}");
		}


		private void TimerHandler(object state)
		{
			lock (timer)
			{
				if (disposed) return;

				int progressBlockCount = (int)(currentProgress * blockCount);
				int percent = (int)(currentProgress * 100);
				string text = string.Format("[{0}{1}] {2,3}% {3}",
					new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
					percent,
					animation[animationIndex++ % animation.Length]);
				UpdateText(text);

				ResetTimer();
			}
		}

		private void UpdateText(string text)
		{
			Console.Clear();
            Console.SetCursorPosition(0, Console.CursorTop);
			Console.WriteLine(text);
			Console.WriteLine(currentStatus);
			Console.Write($"Осталось времени: {leftTime}");
		}

		private void ResetTimer()
		{
			timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
		}

		public void Dispose()
		{
			lock (timer)
			{
				disposed = true;
				UpdateText(string.Empty);
			}
		}

	}
}
