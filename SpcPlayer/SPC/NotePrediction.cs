using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    /// <summary>
    /// Class that provides static methods for determining what note a voice is key-on'd at, given a specific frequency
    /// for C-5
    /// </summary>
    internal class NotePrediction
    {
        private const int NUM_OCTAVES = 10;     // the number of octaves the table supports, the first note being C-0
        private const int LUT_SIZE = NUM_OCTAVES * 12;
        private const int TABLE_MIDLINE = 12 * 5;       // the entry in the table that corresponds to C-5

        private static float[] keyFrequenctRatioLUT = new float[LUT_SIZE];
        private static string[] keyNames =
        {
            "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#", "A-", "A#", "B-"
        };
        private static string[] keyNameLUT = new string[LUT_SIZE];

        static NotePrediction()
        {
            // build the list of ratios for the distance from middle C (C-5), as well as the names of all the notes
            for (int i = 0; i < LUT_SIZE; i++)
            {
                // calculate the distance in semitones from the midline of C-5
                int dist = i - TABLE_MIDLINE;

                // plug it into a formula to get the frequency ratio
                // 2 ^ ( distance / 12 )
                keyFrequenctRatioLUT[i] = (float)Math.Pow(2, dist / 12f);
                // generate the key name
                keyNameLUT[i] = string.Format("{0}{1}",
                    keyNames[i % 12],
                    i / 12);
            }
        }

        public static string GetNoteName(int frequencyBase, int playbackFrequency)
        {
            // calculate the ratio between the playback and the base
            float ratio = playbackFrequency / (float)frequencyBase;
            int closestEntry = 0;

            // iterate through the ratio LUT until we find the first entry larger than the ratio
            for (int i = 0; i < LUT_SIZE; i++)
            {
                // once we've reached a higher table entry, break out of the loop
                if (keyFrequenctRatioLUT[i] > ratio)
                {
                    closestEntry = i;
                    break;
                }
            }

            // if the first entry is already larger than the ratio, that is the closest
            if (closestEntry != 0)
            {
                // compare this entry with the previous entry, and select whichever is closest
                float diff1 = ratio - keyFrequenctRatioLUT[closestEntry - 1];
                float diff2 = Math.Abs(ratio - keyFrequenctRatioLUT[closestEntry]);

                // if diff1 is smaller than diff2, the closest entry is the previous
                if (diff1 < diff2) closestEntry--;
            }

            // return the name of the correct note
            return keyNameLUT[closestEntry];
        }
    }
}
