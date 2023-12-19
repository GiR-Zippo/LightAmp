using System;

namespace BardMusicPlayer.Ui.Functions
{
    public static class HelperFunctions
    {
        /// <summary>
        /// Converts a TimeSpan to a string with Minutes:Seconds
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string TimeSpanToString(TimeSpan time)
        {
            string Seconds = time.Seconds.ToString();
            string Minutes = time.Minutes.ToString();
            return ((Minutes.Length == 1) ? "0" + Minutes : Minutes) + ":" + ((Seconds.Length == 1) ? "0" + Seconds : Seconds);
        }
    }
}
