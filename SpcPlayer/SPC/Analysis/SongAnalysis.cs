using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpcPlayer.SPC;
using SpcPlayer.SPC.Logging;

namespace SpcPlayer.SPC.Analysis
{
    /// <summary>
    /// Class that encapsulates all of the analysis performed on a song, as well as user comments and settings.
    /// </summary>
    internal class SongAnalysis
    {
        // internal struct for tracking the settings of each channel from the playback log
        private class Voice
        {
            public bool enableADSR;
            public int attackRate;
            public int decayRate;
            public int sustainLevel;
            public int sustainRate;
            public int volumeLeft;
            public int volumeRight;
            public bool echoEnabled;
            public bool noiseEnabled;
            public bool pitchModEnabled;
            public GainMode gainMode;
            public int gainParameter;
        }

        // list that stores the timeline of notes
        List<Note> _notes = new List<Note>();
        // array that stores information on all the sources
        Source[] _sources = new Source[256];

        /// <summary>
        /// Creates a new instance of this class using the events recorded in the provided DSP's event log.
        /// </summary>
        /// <param name="eventLog"></param>
        public SongAnalysis(DSP dsp)
        {
            // check arguments
            ArgumentNullException.ThrowIfNull(dsp);

            EventLogger eventLog = dsp.EventLog;

            // create an array of structs for tracking the settings of each channel from the playback log
            Voice[] _voices = new Voice[8];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = new Voice();
            }

            // create the array of source analysis objects
            for (int i = 0; i < 256; i++)
            {
                _sources[i] = new Source();
            }

            // loop through the event log and parse its contents
            for (int eventIndex = 0; eventIndex < eventLog.Count; eventIndex++)
            {
                BaseEvent e = eventLog.Events(eventIndex);

                // switch based on event type
                switch (e.EventType)
                {
                    case EventType.AttackDecayChange:
                        AttackDecayChangeEvent adcEvent = (AttackDecayChangeEvent)e;
                        _voices[adcEvent.Channel].enableADSR = adcEvent.EnableADSR;
                        _voices[adcEvent.Channel].attackRate = adcEvent.AttackRate;
                        _voices[adcEvent.Channel].decayRate = adcEvent.DecayRate;
                        break;

                    case EventType.SustainChange:
                        SustainChangeEvent scEvent = (SustainChangeEvent)e;
                        _voices[scEvent.Channel].sustainLevel = scEvent.SustainLevel;
                        _voices[scEvent.Channel].sustainRate = scEvent.SustainRate;
                        break;

                    case EventType.PitchChange:
                        PitchChangeEvent pcEvent = (PitchChangeEvent)e;

                        // TODO: Log it
                        break;

                    case EventType.VolumeChange:
                        VolumeChangeEvent vcEvent = (VolumeChangeEvent)e;
                        if (vcEvent.IsLeftVolume)
                            _voices[vcEvent.Channel].volumeLeft = vcEvent.Volume;
                        else
                            _voices[vcEvent.Channel].volumeRight = vcEvent.Volume;

                        // TODO: Log it
                        break;

                    case EventType.KeyOn:
                        // search ahead for the next event that either explicitly or implicitly defines
                        // how long this note will play
                        KeyOnEvent konEvent = (KeyOnEvent)e;
                        BaseEvent e2;
                        bool foundEndEvent = false;
                        int cycleOfEndEvent = 0;

                        // mark the corresponding sourceIndex as being used in the song
                        Source source = _sources[konEvent.SourceIndex];
                        Voice voice = _voices[konEvent.Channel];

                        source.IsUsed = true;
                        // log the frequency the voice had at KeyOn for this source
                        source.LogKeyOnFrequency(konEvent.Frequency);

                        // if ADSR is enabled when the note starts, record that ADSR value as being used for the given sourceIndex
                        if (_voices[konEvent.Channel].enableADSR)
                        {
                            source.LogADSR(voice.attackRate, voice.decayRate, voice.sustainLevel, voice.sustainRate);
                        }

                        for (int i = eventIndex + 1; i < eventLog.Count; i++)
                        {
                            e2 = eventLog.Events(i);

                            switch (e2.EventType)
                            {
                                case EventType.KeyOn:
                                case EventType.KeyOff:
                                case EventType.SourceEnd:
                                case EventType.VoiceFaded:
                                    // the note can be implicitly ended by a new Key-On event for the same channel
                                    // or explicitly by a Key-Off, VoiceFaded, or SourceEnd event for the same channel.
                                    // As these are all ChannelEvents, we can cast to that specifically
                                    ChannelEvent endEvent = (ChannelEvent)e2;

                                    if (konEvent.Channel == endEvent.Channel)
                                    {
                                        cycleOfEndEvent = endEvent.CycleTime;
                                        foundEndEvent = true;
                                    }
                                    break;
                            }

                            // break out of the loop if a corresponding ending event is found
                            if (foundEndEvent) break;
                        }

                        // if an end event was not found, the note implicitly ends at the end of playback
                        if (!foundEndEvent)
                        {
                            cycleOfEndEvent = dsp.TotalCycles;
                        }
                        
                        // add the note to the timeline
                        _notes.Add(new Note(
                            konEvent.CycleTime,
                            cycleOfEndEvent - konEvent.CycleTime,
                            konEvent.Channel,
                            konEvent.SourceIndex,
                            konEvent.Frequency,
                            konEvent.LeftVolume,
                            konEvent.RightVolume));
                        break;

                    case EventType.KeyOff:
                    case EventType.SourceEnd:
                    case EventType.VoiceFaded:
                        // these events are handled within the KeyOn event
                        break;

                    case EventType.ChannelEchoChange:
                        ChannelEchoChangeEvent cecEvent = (ChannelEchoChangeEvent)e;

                        _voices[cecEvent.Channel].echoEnabled = cecEvent.EchoEnabled;
                        // TODO: Log it
                        break;

                    case EventType.ChannelNoiseChange:
                        ChannelNoiseChangeEvent cncEvent = (ChannelNoiseChangeEvent)e;

                        _voices[cncEvent.Channel].noiseEnabled = cncEvent.NoiseEnabled;
                        // TODO: Log it
                        break;

                    case EventType.ChannelPitchModChange:
                        ChannelPitchModChangeEvent cpmcEvent = (ChannelPitchModChangeEvent)e;

                        _voices[cpmcEvent.Channel].pitchModEnabled = cpmcEvent.PitchModEnabled;
                        // TODO: Log it
                        break;

                    case EventType.GainChange:
                        GainChangeEvent gcEvent = (GainChangeEvent)e;

                        _voices[gcEvent.Channel].gainMode = gcEvent.GainMode;
                        _voices[gcEvent.Channel].gainParameter = gcEvent.GainParameter;
                        // TODO: Log it
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Unhandled event type {0}", e.EventType));
                }
            }


        }

        /// <summary>
        /// Saves the song analysis to a file so that it can be loaded later.
        /// </summary>
        /// <param name="filename"></param>
        public void SaveToFile(string filename)
        {
            // TODO: This
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of notes in the timeline.
        /// </summary>
        public int NoteCount => _notes.Count;

        /// <summary>
        /// Gets a Note from the timeline using the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Note Notes(int index)
        {
            // check arguments
            if (index < 0 || index >= _notes.Count) throw new ArgumentOutOfRangeException("index");

            return _notes[index];
        }

        /// <summary>
        /// Gets the index of the first note that plays that is active at the specified time.
        /// Returns -1 if no notes are active during or after that timestamp.
        /// </summary>
        /// <param name="cycleTime"></param>
        /// <returns></returns>
        public int GetIndexOfFirstNoteAtTime(int cycleTime)
        {
            // loop through all the notes in the timeline
            for (int i = 0; i < _notes.Count; i++)
            {
                Note note = _notes[i];

                // return if the note's start is equal to or after timestamp
                if (note.StartTime >= cycleTime) return i;

                // return if the note's end time is equal to or after the timestamp
                if (note.StartTime + note.Duration >= cycleTime) return i;
            }

            return -1;
        }
    }
}
