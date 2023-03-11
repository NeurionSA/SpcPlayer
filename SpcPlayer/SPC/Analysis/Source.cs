using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Analysis
{
    /// <summary>
    /// Class for encapsulating all the analysis collected for a SourceIndex, as well as user comments and settings.
    /// </summary>
    internal class Source
    {
        private bool _isUsed = false;   // whether the source is used in the song

        // list of the ADSR settings the source was seen using
        private List<SourceADSR> _ADSRList = new List<SourceADSR>();   

        // list of frequencies used at KeyOn for this source
        private List<int> _freqList = new List<int>();

        /// <summary>
        /// Logs the source as having used specific ADSR settings.
        /// </summary>
        /// <param name="attackRate"></param>
        /// <param name="decayRate"></param>
        /// <param name="sustainLevel"></param>
        /// <param name="sustainRate"></param>
        public void LogADSR(int attackRate, int decayRate, int sustainLevel, int sustainRate)
        {
            // check if the list already has an entry whose settings match
            for (int i = 0; i < _ADSRList.Count; i++)
            {
                // return if a match is found
                SourceADSR s = _ADSRList[i];
                if ((s.AttackRate == attackRate) &&
                    (s.DecayRate == decayRate) &&
                    (s.SustainLevel == sustainLevel) &&
                    (s.SustainRate == sustainRate)) return;
            }

            // no match found, add a new entry
            _ADSRList.Add(new SourceADSR(attackRate, decayRate, sustainLevel, sustainRate));
        }

        /// <summary>
        /// Logs a frequency the source was seen using at KeyOn.
        /// </summary>
        /// <param name="frequency"></param>
        public void LogKeyOnFrequency(int frequency)
        {
            // return if the given frequency was already logged
            if (_freqList.Contains(frequency)) return;

            // add the frequency
            _freqList.Add(frequency);
            // sort the list
            _freqList.Sort();
        }

        public bool IsUsed
        {
            get => _isUsed;
            set => _isUsed = value;
        }

        public int CountADSR => _ADSRList.Count;
        public SourceADSR ADSRs(int index)
        {
            // check arguments
            if (index < 0 || index >= _ADSRList.Count) throw new ArgumentOutOfRangeException("index");

            return _ADSRList[index];
        }
    }
}
