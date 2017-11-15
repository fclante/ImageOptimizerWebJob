﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace ImageOptimizerWebJob
{
    public class Compressor
    {
        string _cwd;

        public Compressor()
        {
            _cwd = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"Tools\");
        }

        public CompressionResult CompressFile(string fileName, bool lossy)
        {
            string targetFile = Path.ChangeExtension(Path.GetTempFileName(), Path.GetExtension(fileName));

            ProcessStartInfo start = new ProcessStartInfo("cmd")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = _cwd,
                Arguments = GetArguments(fileName, targetFile, lossy),
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var stopwatch = Stopwatch.StartNew();

            using (var process = Process.Start(start))
            {
                process.WaitForExit();
            }

            stopwatch.Stop();

            return new CompressionResult(fileName, targetFile, stopwatch.Elapsed);
        }

        private static string GetArguments(string sourceFile, string targetFile, bool lossy)
        {
            if (!Uri.IsWellFormedUriString(sourceFile, UriKind.RelativeOrAbsolute) && !File.Exists(sourceFile))
                return null;

            string ext;

            try
            {
                ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex);
                return null;
            }

            switch (ext)
            {
                case ".png":
                    File.Copy(sourceFile, targetFile);

                    if (lossy)
                        return string.Format(CultureInfo.CurrentCulture, "/c pingo -s8 -q -palette=79 \"{0}\"", targetFile);
                    else
                        return string.Format(CultureInfo.CurrentCulture, "/c pingo -s8 -q \"{0}\"", targetFile);

                case ".jpg":
                case ".jpeg":
                    if (lossy)
                    {
                        return string.Format(CultureInfo.CurrentCulture, "/c cjpeg -quality 80,60 -dct float -smooth 5 -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);
                    }

                    return string.Format(CultureInfo.CurrentCulture, "/c jpegtran -copy none -optimize -progressive -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);

                case ".gif":
                    return string.Format(CultureInfo.CurrentCulture, "/c gifsicle -O3 --batch --colors=256 \"{0}\" --output=\"{1}\"", sourceFile, targetFile);
            }

            return null;
        }
    }
}
