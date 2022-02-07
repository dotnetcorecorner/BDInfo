using DiscUtils;
using System;
using System.IO;

namespace BDExtractor
{
	public delegate void ProgressChangeDelegate(double percentage);
	public delegate void Completedelegate();

	internal sealed class FileCopier
	{
		private readonly DiscFileInfo _sourceFilePath;
		private readonly string _destFilePath;
		private readonly int _buffer;
		private const int DEFAULT_BUFFER = 1024 * 1024;

		public FileCopier(DiscFileInfo file, string destFile, int bufferBytes = DEFAULT_BUFFER)
		{
			if (string.IsNullOrWhiteSpace(destFile))
			{
				throw new ArgumentException($"'{nameof(destFile)}' cannot be null or whitespace.", nameof(destFile));
			}

			_sourceFilePath = file ?? throw new ArgumentNullException(nameof(file));
			_destFilePath = destFile;
			_buffer = bufferBytes;

			OnProgressChanged += delegate { };
			OnComplete += delegate { };
		}

		public event ProgressChangeDelegate OnProgressChanged;
		public event Completedelegate OnComplete;

		public void Copy()
		{
			byte[] buffer = new byte[_buffer];
			bool cancelFlag = false;

			using (Stream source = _sourceFilePath.OpenRead())
			{
				long fileLength = source.Length;
				using (FileStream dest = new FileStream(_destFilePath, FileMode.CreateNew, FileAccess.Write))
				{
					long totalBytes = 0;
					int currentBlockSize = 0;

					while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
					{
						totalBytes += currentBlockSize;
						double persentage = (double)totalBytes * 100.0 / fileLength;

						dest.Write(buffer, 0, currentBlockSize);

						cancelFlag = false;
						OnProgressChanged(persentage);

						if (cancelFlag == true)
						{
							break;
						}
					}
				}
			}

			if (cancelFlag)
			{
				try
				{
					File.Delete(_destFilePath);
				}
				catch { }
			}

			OnComplete();
		}
	}
}