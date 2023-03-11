using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Analysis
{
    /// <summary>
    /// Encapsulates observed ADSR settings for a voice source.
    /// </summary>
    internal class SourceADSR
    {
        private int _attackRate;
        private int _decayRate;
        private int _sustainLevel;
        private int _sustainRate;

        public SourceADSR(int attackRate, int decayRate, int sustainLevel, int sustainRate)
        {
            _attackRate = attackRate;
            _decayRate = decayRate;
            _sustainLevel = sustainLevel;
            _sustainRate = sustainRate;
        }

        public int AttackRate => _attackRate;
        public int DecayRate => _decayRate;
        public int SustainLevel => _sustainLevel;
        public int SustainRate => _sustainRate;
    }
}
