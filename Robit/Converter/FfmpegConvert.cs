using Xabe.FFmpeg;

namespace Robit.Converter
{
    public static class FfmpegConvert
    {
        /// <summary>
        ///     Converts a file from one format to another format using ffmpeg
        /// </summary>
        /// 
        /// <param name="path">
        ///     Path to the file
        /// </param>
        /// 
        /// <param name="formatFrom">
        ///     Original format of the file
        /// </param>
        /// 
        /// <param name="formatTo">
        ///     Desired format of the file
        /// </param>
        public static void Convert(string path, string formatFrom, string formatTo)
        {
            string arguments = $"-i \"{path}/download.{formatFrom}\" \"{path}/output.{formatTo}\"";

            FFmpeg.Conversions.New().Start(arguments).Wait();
        }
    }
}
