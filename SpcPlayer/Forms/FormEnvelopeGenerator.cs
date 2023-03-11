using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpcPlayer.Forms
{
    public partial class FormEnvelopeGenerator : Form
    {
        // number of native (32 Khz) samples per counter event
        private static int[] counterRates =
        {
            0, 2048, 1536, 1280, 1024, 768, 640, 512,
            384, 320, 256, 192, 160, 128, 96, 80,
            64, 48, 40, 32, 24, 20, 16, 12,
            10, 8, 6, 5, 4, 3, 2, 1
        };

        // flags for whether the contents of the 4 parameter fields are valid
        private bool _arValid = true;
        private bool _drValid = true;
        private bool _slValid = true;
        private bool _srValid = true;
        private bool _tpsValid = true;

        private int _attackRate = 0xF;
        private int _decayRate = 0;
        private int _sustainLevel = 0x7;
        private int _sustainRate = 0x1A;
        private int _ticksPerSecond = 96;

        private List<int> _tickPoints = new List<int>();
        private List<int> _envelopePoints = new List<int>();

        // amount volume must change by before next point sampling in the decay and sustain phases
        int _granularity = 4;

        public FormEnvelopeGenerator()
        {
            InitializeComponent();

            // replace the contents of the text fields with the initial values of the ints
            txtAttackRate.Text = string.Format("{0:X}", _attackRate);
            txtDecayRate.Text = string.Format("{0:X}", _decayRate);
            txtSustainLevel.Text = string.Format("{0:X}", _sustainLevel);
            txtSustainRate.Text = string.Format("{0:X}", _sustainRate);
            txtTicksPerSecond.Text = string.Format("{0}", _ticksPerSecond);
        }

        private void generateEnvelope()
        {
            // clear the list of data points
            _tickPoints.Clear();
            _envelopePoints.Clear();

            int cycleCounter = 0;
            int envelope = 0x7FF;

            int lastTickPoint = 0;
            int lastEnvelopePoint = 64;

            // if AttackRate is 0xF, then we just start at full envelope
            if (_attackRate == 0xF)
            {
                // add the first point
                _tickPoints.Add(0);
                _envelopePoints.Add(64);
            }
            else
            {
                // first point is 0,0
                _tickPoints.Add(0);
                _envelopePoints.Add(0);

                // calculate how many clock cycles it'd take to reach full volume
                cycleCounter = 64 * counterRates[(_attackRate << 1) + 1];

                // add the data point for the linear increase to full volume
                _tickPoints.Add((cycleCounter * _ticksPerSecond / 32000) + 1);
                _envelopePoints.Add(64);
                lastTickPoint = (cycleCounter * _ticksPerSecond / 32000) + 1;
            }

            // now we enter some loops for the exponential parts

            // we do the decay to sustain level loop if SL != 7
            if (_sustainLevel != 7)
            {
                while (envelope >> 8 != _sustainLevel)
                {
                    cycleCounter += counterRates[(_decayRate << 1) + 0x10];

                    envelope -= ((envelope - 1) >> 8) + 1;

                    // determine what the envelope point and tick value would be here
                    int envPoint = (envelope + 1) >> 5;
                    int tickPoint = (int)(cycleCounter / 32000f * _ticksPerSecond) + 1;

                    // if the new point's difference from the previous logged point >= the granularity,
                    // AND at least 1 tick has elapsed, log it
                    if ((lastEnvelopePoint - envPoint >= _granularity) && (tickPoint > lastTickPoint))
                    {
                        lastEnvelopePoint = envPoint;
                        lastTickPoint = tickPoint;
                        _envelopePoints.Add(envPoint);
                        _tickPoints.Add(tickPoint);
                    }
                }
            }

            // we do the final loop if SR != 0 (in which case the note plays until key-off)
            if (_sustainRate != 0)
            {
                while (envelope > 0)
                {
                    // advance to the next envelope change
                    cycleCounter += counterRates[_sustainRate];

                    envelope -= ((envelope - 1) >> 8) + 1;

                    // determine what the envelope point and tick value would be here
                    int envPoint = (envelope + 1) >> 5;
                    int tickPoint = (int)(cycleCounter / 32000f * _ticksPerSecond) + 1;

                    // if the new point's difference from the previous logged point >= the granularity,
                    // AND at least 1 tick has elapsed, log it
                    // OR log if the envelope is now 0 AND the previous envelope point is not 0
                    if (((lastEnvelopePoint - envPoint >= _granularity) && (tickPoint > lastTickPoint)) ||
                        ((envelope == 0) && (_envelopePoints[_envelopePoints.Count - 1] != 0)))
                    {
                        lastEnvelopePoint = envPoint;
                        lastTickPoint = tickPoint;
                        _envelopePoints.Add(envPoint);
                        _tickPoints.Add(tickPoint);
                    }
                }
            }

            // build the string that is copied to the clipboard
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("ModPlug Tracker Envelope");
            sb.AppendLine(string.Format("{0},{1},{1},0,0,0,0,0", _envelopePoints.Count, _envelopePoints.Count - 1));
            for (int i = 0; i < _envelopePoints.Count; i++)
            {
                sb.AppendLine(string.Format("{0},{1}", _tickPoints[i], _envelopePoints[i]));
            }
            sb.AppendLine("255");
            sb.AppendLine();

            Console.WriteLine(sb.ToString());
            Clipboard.SetText(sb.ToString());
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            generateEnvelope();
        }

        private void updateUIForTextBoxValidity(TextBox textBox, bool isValid)
        {
            // first change the text box's back color to reflect its validity
            if (isValid)
            {
                textBox.BackColor = SystemColors.Window;
            }
            else
            {
                textBox.BackColor = Color.Red;
            }

            // enable/disable the generate button
            if (_arValid && _drValid && _slValid && _srValid && _tpsValid)
            {
                btnGenerate.Enabled = true;
            }
            else
            {
                btnGenerate.Enabled = false;
            }
        }

        private void txtAttackRate_TextChanged(object sender, EventArgs e)
        {
            // try to cast it
            try
            {
                _attackRate = Convert.ToInt32(txtAttackRate.Text, 16);
            }
            catch
            {
                // doesn't matter what exception we get, we mark the field as invalid and leave
                _arValid = false;
                updateUIForTextBoxValidity((TextBox)sender, _arValid);
                return;
            }

            // make sure the value entered is in the proper range
            _arValid = (_attackRate >= 0) && (_attackRate <= 0xF) ? true : false;
            updateUIForTextBoxValidity((TextBox)sender, _arValid);
        }

        private void txtDecayRate_TextChanged(object sender, EventArgs e)
        {
            // try to cast it
            try
            {
                _decayRate = Convert.ToInt32(txtDecayRate.Text, 16);
            }
            catch
            {
                // doesn't matter what exception we get, we mark the field as invalid and leave
                _drValid = false;
                updateUIForTextBoxValidity((TextBox)sender, _drValid);
                return;
            }

            // make sure the value entered is in the proper range
            _drValid = (_decayRate >= 0) && (_decayRate <= 0x7) ? true : false;
            updateUIForTextBoxValidity((TextBox)sender, _drValid);
        }

        private void txtSustainLevel_TextChanged(object sender, EventArgs e)
        {
            // try to cast it
            try
            {
                _sustainLevel = Convert.ToInt32(txtSustainLevel.Text, 16);
            }
            catch
            {
                // doesn't matter what exception we get, we mark the field as invalid and leave
                _slValid = false;
                updateUIForTextBoxValidity((TextBox)sender, _slValid);
                return;
            }

            // make sure the value entered is in the proper range
            _slValid = (_sustainLevel >= 0) && (_sustainLevel <= 0x7) ? true : false;
            updateUIForTextBoxValidity((TextBox)sender, _slValid);
        }

        private void txtSustainRate_TextChanged(object sender, EventArgs e)
        {
            // try to cast it
            try
            {
                _sustainRate = Convert.ToInt32(txtSustainRate.Text, 16);
            }
            catch
            {
                // doesn't matter what exception we get, we mark the field as invalid and leave
                _srValid = false;
                updateUIForTextBoxValidity((TextBox)sender, _srValid);
                return;
            }

            // make sure the value entered is in the proper range
            _srValid = (_sustainRate >= 0) && (_sustainRate <= 0x1F) ? true : false;
            updateUIForTextBoxValidity((TextBox)sender, _srValid);
        }

        private void txtTicksPerSecond_TextChanged(object sender, EventArgs e)
        {
            // try to cast it
            try
            {
                _ticksPerSecond = Convert.ToInt32(txtTicksPerSecond.Text);
            }
            catch
            {
                // doesn't matter what exception we get, we mark the field as invalid and leave
                _tpsValid = false;
                updateUIForTextBoxValidity((TextBox)sender, _tpsValid);
                return;
            }

            // make sure the value entered is in the proper range
            _tpsValid = (_ticksPerSecond >= 1) ? true : false;
            updateUIForTextBoxValidity((TextBox)sender, _tpsValid);
        }
    }
}
