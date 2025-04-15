using System.Text.RegularExpressions;

namespace WatchParty.WS.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Splits the command string into parameters, excluding the command itself.
        /// </summary>
        /// <param name="command">The command string.</param>
        /// <returns>An array of command parameters.</returns>
        public static string[] GetCommandParams(this string command)
        {
            var commandParams = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return commandParams.Length < 2 ? Array.Empty<string>() : commandParams.Skip(1).ToArray();
        }

        /// <summary>
        /// Validates if the string is a YouTube URL.
        /// </summary>
        /// <param name="url">The URL string to validate.</param>
        /// <returns>The URL if valid; otherwise, an empty string.</returns>
        public static string TryConvertToYoutubeUrl(this string url)
        {
            string youtubePattern = @"^(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/|v\/|shorts\/)|youtu\.be\/)([\w\-]{11})(?:\S+)?$";
            var regex = new Regex(youtubePattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(url) ? url : string.Empty;
        }
    }
}
