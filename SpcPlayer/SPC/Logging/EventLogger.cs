using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    /// <summary>
    /// Class for advanced logging of events during SPC playback and converting them into a useful human-readable format.
    /// </summary>
    internal class EventLogger
    {
        // list of BaseEvent objects, sorted in the order they were added
        private List<BaseEvent> _eventList = new List<BaseEvent>();

        // an array of 256 integers that store the frequency of note C-5 for each sample source
        private int[] _sourceBaseFreq = new int[256];

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public EventLogger()
        {

        }

        /// <summary>
        /// Gets the last event added to the list, or returns null if the list is empty.
        /// </summary>
        /// <returns></returns>
        private BaseEvent? getLastEvent()
        {
            if (_eventList.Count == 0) return null;
            return _eventList[_eventList.Count - 1];
        }

        public void SetSourceBaseRate(int sourceIndex, int rate)
        {
            _sourceBaseFreq[sourceIndex] = rate;
        }

        public void LogKeyOn(int clockCycle, int channelIndex, int sourceIndex, int frequency, int leftVolume, int rightVolume)
        {
            // adds a KeyOn event
            _eventList.Add(new KeyOnEvent(
                clockCycle,
                channelIndex,
                sourceIndex,
                frequency,
                leftVolume,
                rightVolume));
        }

        public void LogKeyOff(int clockCycle, int channelIndex)
        {
            // adds a KeyOff event
            _eventList.Add(new KeyOffEvent(clockCycle, channelIndex));
        }

        public void LogPitchChange(int clockCycle, int channelIndex, int frequency)
        {
            // adds a PitchChange event

            // check if there's already a pitch change event for that cycle and that channel,
            // and if there is, discard the old one before adding the new one
            BaseEvent? lastEvent = getLastEvent();
            if (lastEvent?.EventType == EventType.PitchChange)
            {
                PitchChangeEvent pc = (PitchChangeEvent)lastEvent;
                if ((pc.Channel == channelIndex) && (pc.CycleTime == clockCycle))
                {
                    // remove that last event
                    _eventList.RemoveAt(_eventList.Count - 1);
                }
            }
            _eventList.Add(new PitchChangeEvent(clockCycle, channelIndex, frequency));
        }

        public void LogVolumeChange(int clockCycle, int channelIndex, int volume, bool isLeftVolume)
        {
            _eventList.Add(new VolumeChangeEvent(clockCycle, channelIndex, volume, isLeftVolume));
        }

        public void LogAttackDecayChange(int clockCycle, int channelIndex, bool enableADSR, int attackRate, int decayRate)
        {
            _eventList.Add(new AttackDecayChangeEvent(clockCycle, channelIndex, enableADSR, attackRate, decayRate));
        }

        public void LogSustainChange(int clockCycle, int channelIndex, int sustainLevel, int sustainRate)
        {
            _eventList.Add(new SustainChangeEvent(clockCycle, channelIndex, sustainLevel, sustainRate));
        }

        public void LogGainChange(int clockCycle, int channelIndex, GainMode gainMode, int gainParameter)
        {
            _eventList.Add(new GainChangeEvent(clockCycle, channelIndex, gainMode, gainParameter));
        }

        public void LogChannelEchoChange(int clockCycle, int channelIndex, bool echoEnabled)
        {
            _eventList.Add(new ChannelEchoChangeEvent(clockCycle, channelIndex, echoEnabled));
        }

        public void LogChannelNoiseChange(int clockCycle, int channelIndex, bool noiseEnabled)
        {
            _eventList.Add(new ChannelNoiseChangeEvent(clockCycle, channelIndex, noiseEnabled));
        }

        public void LogChannelPitchModChange(int clockCycle, int channelIndex, bool pitchModEnabled)
        {
            _eventList.Add(new ChannelPitchModChangeEvent(clockCycle, channelIndex, pitchModEnabled));
        }

        public void LogSourceEnd(int clockCycle, int channelIndex)
        {
            _eventList.Add(new SourceEndEvent(clockCycle, channelIndex));
        }

        public void LogVoiceFaded(int clockCycle, int channelIndex)
        {
            _eventList.Add(new VoiceFadedEvent(clockCycle, channelIndex));
        }

        /// <summary>
        /// Clears the contents of the event log.
        /// </summary>
        public void Clear()
        {
            _eventList.Clear();
        }

        // common code for adding the timestamp to the start of the line in the generated log
        private void addLogTimestamp(StringBuilder sb, int cycleTime, int lastAddedEventTime)
        {
            // if the cycle times do not match
            if (cycleTime != lastAddedEventTime)
            {
                // handle the special sentinel value for lastAddedEventTime
                if (lastAddedEventTime < 0) lastAddedEventTime = 0;
                // add the time of the event and the delta from the previous
                sb.AppendFormat("cycle {0,9} (+{1,6}) : ", cycleTime, cycleTime - lastAddedEventTime);
            }
            else
            {
                // cycle times match, just pad out the start so everything is nice and aligned visually
                sb.Append("                          : ");
            }
        }

        /// <summary>
        /// Generates and returns the human-readable form of the log as a string.
        /// </summary>
        /// <returns></returns>
        public string GenerateLogString()
        {
            StringBuilder sb = new StringBuilder();
            int cycleOfLastPrintedItem = -1;

            // dictionary for storing lists of pan values per source
            Dictionary<int, List<int>> sourcePanDict = new Dictionary<int, List<int>>();

            // iterate through all the items in the log, which we are assuming is already in chronological order
            // (we don't do a foreach because we want to be able to know our current position in the list and look ahead
            // for future events that correspond to the one we're working on)
            for (int i = 0; i < _eventList.Count; i++)
            {
                BaseEvent e = _eventList[i];

                // switch based on event type
                switch (e.EventType)
                {
                    case EventType.PitchChange:
                        PitchChangeEvent pcEvent = (PitchChangeEvent)e;

                        // add the timestamp
                        addLogTimestamp(sb, e.CycleTime, cycleOfLastPrintedItem);
                        cycleOfLastPrintedItem = e.CycleTime;

                        sb.AppendLine(string.Format("PITCH-CHANGE v{0} = {1} Hz",
                            pcEvent.Channel,
                            pcEvent.Frequency));
                        break;

                    case EventType.VolumeChange:
                        VolumeChangeEvent vcEvent = (VolumeChangeEvent)e;

                        // add the timestamp
                        addLogTimestamp(sb, e.CycleTime, cycleOfLastPrintedItem);
                        cycleOfLastPrintedItem = e.CycleTime;

                        sb.AppendLine(string.Format(
                            "VOL-CHANGE v{0}, {1} = {2}",
                            vcEvent.Channel,
                            vcEvent.IsLeftVolume ? "L" : "R",
                            vcEvent.Volume));
                        break;

                    case EventType.KeyOn:
                        KeyOnEvent konEvent = (KeyOnEvent)e;
                        BaseEvent endEvent;
                        bool foundEndEvent = false;
                        int cycleOfEndEvent = 0;

                        // calculate the volume pan for the key-on event
                        int hiVol = Math.Max(konEvent.LeftVolume, konEvent.RightVolume);
                        int loVol = Math.Min(konEvent.LeftVolume, konEvent.RightVolume);
                        // determine the panning (0 - 255)
                        int pan = 128;
                        if (hiVol > loVol)
                        {
                            float frac = loVol / (float)hiVol;
                            if (konEvent.LeftVolume > konEvent.RightVolume)
                            {
                                // panned to the left
                                pan = (int)(128 * frac);
                            }
                            else
                            {
                                // panned to the right
                                pan = 255 - (int)(128 * frac);
                            }
                        }

                        // add it to the list of pan values for the source
                        if (!sourcePanDict.ContainsKey(konEvent.SourceIndex))
                            sourcePanDict.Add(konEvent.SourceIndex, new List<int>());
                        if (!sourcePanDict[konEvent.SourceIndex].Contains(pan))
                            sourcePanDict[konEvent.SourceIndex].Add(pan);

                        // search ahead for the next event that either explicitly or implicitly defines
                        // how long this note will play
                        for (int j = i + 1; j < _eventList.Count; j++)
                        {
                            endEvent = _eventList[j];
                            // a nested switch statement, oh boy!
                            switch (endEvent.EventType)
                            {
                                case EventType.KeyOn:
                                    // the note can be implicitly ended by a new Key-On event for the same channel
                                    KeyOnEvent konEvent2 = (KeyOnEvent)endEvent;
                                    // if the channels match, that's an ending, baybee
                                    if (konEvent.Channel == konEvent2.Channel)
                                    {
                                        cycleOfEndEvent = konEvent2.CycleTime;
                                        foundEndEvent = true;
                                    }
                                    break;

                                case EventType.KeyOff:
                                    // the note can be explicitly ended by a corresponding Key-Off event
                                    KeyOffEvent koffEvent = (KeyOffEvent)endEvent;
                                    // if the channels match, that's an ending
                                    if (konEvent.Channel == koffEvent.Channel)
                                    {
                                        cycleOfEndEvent = koffEvent.CycleTime;
                                        foundEndEvent = true;
                                    }
                                    break;
                            }

                            // if the event we just checked was the ender, break out of this for loop
                            if (foundEndEvent) break;
                        }

                        // add the timestamp
                        addLogTimestamp(sb, e.CycleTime, cycleOfLastPrintedItem);
                        cycleOfLastPrintedItem = e.CycleTime;

                        // if the sourceIndex has a base frequency defined, output the line formatted to mimic the
                        // appearance of a tracker note
                        if (_sourceBaseFreq[konEvent.SourceIndex] != 0)
                        { 
                            sb.AppendFormat("KEY-ON v{0} : {1} {2,02} v{3:D2} p{4:X2} ... : ",
                                konEvent.Channel,
                                NotePrediction.GetNoteName(_sourceBaseFreq[konEvent.SourceIndex], konEvent.Frequency),
                                konEvent.SourceIndex,
                                Math.Min(hiVol, 64),
                                pan);
                        }
                        else
                        {
                            // output the line less fancy
                            sb.AppendFormat("KEY-ON v{0}, s{1,2}, at {2,5} Hz, vol {3,2}:{4,2} : ",
                                konEvent.Channel,
                                konEvent.SourceIndex,
                                konEvent.Frequency,
                                konEvent.LeftVolume,
                                konEvent.RightVolume);
                        }

                        // now we know IF the event ended and WHEN, so we can log it appropriately
                        if (foundEndEvent)
                        {
                            sb.AppendFormat("plays {0,6} cycles", cycleOfEndEvent - konEvent.CycleTime);
                        }
                        else
                        {
                            // the note never ended, comment as such in the log
                            sb.Append("note did not end");
                        }
                        // finish off the line
                        sb.AppendLine();
                        break;

                    case EventType.KeyOff:
                        // since the handler for Key-On events also looks ahead for the corresponding Key-Off,
                        // we just ignore Key-Off events for now
                        break;
                }
            }

            // append the table of pan values that were seen for each source index
            sb.AppendLine();
            sb.AppendLine("Pan Values seen for each Source Index:");
            foreach (int sourceIndex in sourcePanDict.Keys)
            {
                sb.AppendFormat("source {0}: ", sourceIndex);
                for (int i = 0; i < sourcePanDict[sourceIndex].Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.AppendFormat("{0:X2}", sourcePanDict[sourceIndex][i]);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the number of events in the log.
        /// </summary>
        public int Count => _eventList.Count;

        public BaseEvent Events(int index)
        {
            return _eventList[index];
        }
    }
}
