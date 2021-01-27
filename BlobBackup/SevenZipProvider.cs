using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlobBackup
{
    public class SevenZipProvider
    {
        private static int _ranCleanUp;
        private static string? _tempFolder;

        public static async Task<byte[]> Compress(byte[] bytes) => await Run7Zip(bytes, compress: true);

        public static async Task<byte[]> Decompress(byte[] bytes) => await Run7Zip(bytes, compress: false);

        private static async Task<byte[]> Run7Zip(byte[] bytes, bool compress)
        {
            Initialize();

            var arguments = compress ?
                "a -txz -si -so -mx=9 -ms=on -an" :
                "e -txz -si -so";
            var startInfo = new ProcessStartInfo("7z", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var process = Process.Start(startInfo);

            if (process is null)
            {
                throw new Exception($"Could not run 7-Zip.");
            }

            using var memoryStream = new MemoryStream();
            var writeTask = process.StandardInput.BaseStream.WriteAsync(bytes).AsTask().ContinueWith(_ =>
            {
                process.StandardInput.Close();
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
            await process.StandardOutput.BaseStream.FlushAsync();
            await writeTask;
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Directory.CreateDirectory(TempFolder);
                var path = Path.Combine(TempFolder, $"error-output-{DateTime.UtcNow.Ticks}.txt");
                await File.WriteAllTextAsync(path, await process.StandardError.ReadToEndAsync());

                throw new Exception($"Command '7z {arguments}' exited with code {process.ExitCode}. See '{path}' for more information.");
            }

            return memoryStream.ToArray();
        }

        private static void Initialize()
        {
            if (Directory.Exists(TempFolder))
            {
                RunCleanUp();
            }
        }

        private static void RunCleanUp()
        {
            if (Interlocked.CompareExchange(ref _ranCleanUp, 1, 0) == 0)
            {
                foreach (var path in Directory.GetFiles(TempFolder))
                {
                    var fileInfo = new FileInfo(path);
                    var isLogFile = Path.GetExtension(path) == ".txt";
                    var maxLastWriteTimeUtc = isLogFile ? DateTime.UtcNow.AddDays(-31) : DateTime.UtcNow.AddHours(-1);

                    if (fileInfo.LastWriteTimeUtc < maxLastWriteTimeUtc)
                    {
                        File.Delete(path);
                    }
                }
            }
        }

        private static string TempFolder
        {
            get
            {
                if (_tempFolder is null)
                {
                    _tempFolder = Path.Combine(ServiceProviderProvider.DefaultDataFolder, "7zip-temp");
                }

                return _tempFolder;
            }
        }
    }
}
