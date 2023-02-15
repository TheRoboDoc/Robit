using Xabe.FFmpeg;

namespace Robit.Converter
{
    public static class FfmpegConvert
    {
        public static void Convert(string path, string formatFrom, string formatTo)
        {
            string arguments = $"-i \"{path}/download.{formatFrom}\" \"{path}/output.{formatTo}\"";

            FFmpeg.Conversions.New().Start(arguments).Wait();
        }
    }
}
