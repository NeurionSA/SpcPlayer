using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class AttackDecayChangeEvent : ChannelEvent
    {
        private bool _enableADSR;
        private int _attackRate;
        private int _decayRate;

        public AttackDecayChangeEvent(int cycleTime, int channel, bool enableADSR, int attackRate, int decayRate) : base(EventType.AttackDecayChange, cycleTime, channel)
        {
            _enableADSR = enableADSR;
            _attackRate = attackRate;
            _decayRate = decayRate;
        }

        public bool EnableADSR => _enableADSR;
        public int AttackRate => _attackRate;
        public int DecayRate => _decayRate;
    }
}
