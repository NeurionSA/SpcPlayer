using SpcPlayer.SPC;
using NAudio.Wave;

namespace SpcPlayer
{
    public partial class FormPlayer : Form
    {
        private static Font fontDspReg = new Font("Lucida Console", 9);
        private static Font fontGeneralUI = new Font("Segoe UI", 9);
        private static Brush brushDspReg = new SolidBrush(Color.FromArgb(216, 216, 216));
        private static Brush brushNotDspReg = new SolidBrush(Color.FromArgb(128, 128, 128));

        // the object that handles audio playback
        private SPCPlayer _player;

        // the SPC Core
        private SPCCore _core = new SPCCore(32000);

        // arrays for the DSP register visualizer
        private const int DSP_REG_TIMER_LOAD = 10;      // the value loaded into a timer when a register's value changes
        private byte[] _dspRegs = new byte[128];        // the actual values of the registers
        private int[] _dspRegTimer = new int[128];      // timers for the highlighting of registers when their values change

        public FormPlayer()
        {
            InitializeComponent();

            _player = new SPCPlayer(_core);
            _player.PlaybackStopped += _player_PlaybackStopped;
        }

        private void loadSPC()
        {
            // is only valid when playback is stopped
            if (_player.Status != PlaybackState.Stopped) return;

            // show the file open dlg
            DialogResult result = odlgOpenSpc.ShowDialog();
            // if a file wasn't selected the nexit
            if (result != DialogResult.OK) return;

            // attempt to load the SPC
            _player.LoadSPC(odlgOpenSpc.FileName);

            // enable the play button
            btnPlay.Enabled = true;
            // enable the visual update timer
            tmrUpdateVisuals.Enabled = true;
        }

        private void playSPC()
        {
            btnOpen.Enabled = false;
            btnPlay.Enabled = false;
            btnPause.Enabled = true;
            btnStop.Enabled = true;

            // clear the console if the player was previously stopped
            if (_player.Status == PlaybackState.Stopped)
            {
                Console.Clear();
            }
            //_core.DSP.EnableHeavyLogging = true;
            _player.Play();
        }

        private void pauseSPC()
        {
            btnPlay.Enabled = true;
            btnPause.Enabled = false;

            _player.Pause();
        }

        private void stopSPC()
        {
            _player.Stop();
        }

        private void drawDspRegisters(Graphics g)
        {
            const int CELL_WIDTH = 20;
            const int CELL_HEIGHT = 16;
            const int BORDER_TOP = 2;
            const int BORDER_LEFT = 2;

            // erase the BG to black
            g.Clear(Color.Black);

            // display it like its done in SPC Tool

            // draw the Nx labels along the top
            for (int i = 0; i < 8; i++)
            {
                string s = string.Format("{0:X}x", i);
                SizeF size = g.MeasureString(s, fontDspReg);

                g.DrawString(
                    s,
                    fontDspReg,
                    Brushes.White,
                    (CELL_WIDTH - size.Width) / 2 + (i + 1) * CELL_WIDTH + BORDER_LEFT,
                    (CELL_HEIGHT - size.Height) / 2 + BORDER_TOP);
            }
            for (int i = 0; i < 16; i++)
            {
                String s = string.Format("x{0:X}", i);
                SizeF size = g.MeasureString(s, fontDspReg);

                g.DrawString(
                    s,
                    fontDspReg,
                    Brushes.White,
                    (CELL_WIDTH - size.Width) / 2 + BORDER_LEFT,
                    (CELL_HEIGHT - size.Height) / 2 + (i + 1) * CELL_HEIGHT + BORDER_TOP);
            }

            // draw the cell contents
            for (int i = 0; i < 128; i++)
            {
                int x = ((i >> 4) + 1) * CELL_WIDTH + BORDER_LEFT;
                int y = ((i % 16) + 1) * CELL_HEIGHT + BORDER_TOP;

                String s = string.Format("{0:X2}", _dspRegs[i]);
                SizeF size = g.MeasureString(s, fontDspReg);

                float frac = _dspRegTimer[i] / (float)DSP_REG_TIMER_LOAD;

                // paint the background first
                Color bgColor = Color.FromArgb((int)(0 * frac), (int)(128 * frac), (int)(0 * frac));
                SolidBrush sb = new SolidBrush(bgColor);
                Rectangle fill = new Rectangle(x, y, CELL_WIDTH, CELL_HEIGHT);
                Brush textColor;
                
                g.FillRectangle(sb, fill);

                // override the text color if it's one of the unused registers
                // (xA, xB, xE, and 1D)
                if (((i & 0xF) == 0xA) || ((i & 0xF) == 0xB) || ((i & 0xF) == 0xE) || (i == 0x1D))
                    textColor = brushNotDspReg;
                else
                    textColor = brushDspReg;

                // draw the text over it, centered in the cell
                g.DrawString(
                    s,
                    fontDspReg,
                    textColor,
                    (CELL_WIDTH - size.Width) / 2 + x,
                    (CELL_HEIGHT - size.Height) / 2 + y);

                // free the brush
                sb.Dispose();
            }
        }

        private void drawVoiceVolumesRaw(Graphics g)
        {
            const int CELL_WIDTH = 28;
            const int CELL_HEIGHT = 16;
            const int BORDER_TOP = 2;
            const int BORDER_LEFT = 0;

            // erase the BG to black
            g.Clear(Color.Black);

            // display it like its done in SPC Tool
            // write out the left and right volumes for the 8 voices
            for (int voiceIndex = 0; voiceIndex < 8; voiceIndex++)
            {
                for (int i = 0; i < 2; i++)
                {
                    string s = string.Format("{0}", (sbyte)_core.DSP.GetRegisterDirect((voiceIndex << 4) + i));
                    SizeF size = g.MeasureString(s, fontGeneralUI);

                    g.DrawString(
                        s,
                        fontGeneralUI,
                        Brushes.LightGray,
                        (CELL_WIDTH - size.Width) + (i * CELL_WIDTH) + BORDER_LEFT,
                        (CELL_HEIGHT - size.Height) / 2 + (voiceIndex * CELL_HEIGHT) + BORDER_TOP);
                }
            }

            // write out the main and echo volumes for Left and Right channels
            for (int cIndex = 0; cIndex < 2; cIndex++)
            {
                for (int i = 0; i < 2; i++)
                {
                    string s = string.Format("{0}", (sbyte)_core.DSP.GetRegisterDirect(0xC + (((i << 1) + cIndex) << 4)));

                    SizeF size = g.MeasureString(s, fontGeneralUI);

                    g.DrawString(
                        s,
                        fontGeneralUI,
                        Brushes.LightGray,
                        (CELL_WIDTH - size.Width) + (i * CELL_WIDTH) + BORDER_LEFT,
                        (CELL_HEIGHT - size.Height) / 2 + (cIndex * CELL_HEIGHT) + BORDER_TOP + CELL_HEIGHT * 8.5f);
                }
            }
        }

        private void drawVoiceMeters(Graphics g)
        {
            const int CELL_HEIGHT = 16;
            const int CELL_WIDTH = 16;
            const int BORDER_TOP = 2;
            const int BORDER_LEFT = 2;
            const int METER_WIDTH = 188;

            string s;
            SizeF size;

            // erase the BG to black
            g.Clear(Color.Black);

            // draw the labels and meters for the voices
            for (int voiceIndex = 0; voiceIndex < 8; voiceIndex++)
            {
                s = string.Format("{0}", voiceIndex);
                size = g.MeasureString(s, fontGeneralUI);

                g.DrawString(
                    s,
                    fontGeneralUI,
                    Brushes.LightGray,
                    (CELL_WIDTH - size.Width) / 2 + BORDER_LEFT,
                    (CELL_HEIGHT - size.Height) / 2 + (voiceIndex * CELL_HEIGHT) + BORDER_TOP);

                // draw the meters
                float vuLeft = _core.DSP.GetVoiceMeterLeft(voiceIndex) * METER_WIDTH;
                float vuRight = _core.DSP.GetVoiceMeterRight(voiceIndex) * METER_WIDTH;

                // left first
                g.FillRectangle(Brushes.Green,
                    CELL_WIDTH + BORDER_LEFT,
                    voiceIndex * CELL_HEIGHT + BORDER_TOP + 1,
                    vuLeft,
                    5);
                // right next
                g.FillRectangle(Brushes.Green,
                    CELL_WIDTH + BORDER_LEFT,
                    voiceIndex * CELL_HEIGHT + BORDER_TOP + 7,
                    vuRight,
                    5);
            }

            // draw the labels and meters for the main output left and right
            for (int i = 0; i < 2; i++)
            {
                float vu;
                if (i == 0)
                {
                    s = "L";
                    vu = _core.DSP.GetMainMeterLeft() * METER_WIDTH;
                }
                else
                {
                    s = "R";
                    vu = _core.DSP.GetMainMeterRight() * METER_WIDTH;
                }

                size = g.MeasureString(s, fontGeneralUI);

                g.DrawString(
                    s,
                    fontGeneralUI,
                    Brushes.LightGray,
                    (CELL_WIDTH - size.Width) / 2 + BORDER_LEFT,
                    (CELL_HEIGHT - size.Height) / 2 + (i * CELL_HEIGHT) + BORDER_TOP + CELL_HEIGHT * 8.5f);

                // draw the meter
                g.FillRectangle(Brushes.Green,
                    CELL_WIDTH + BORDER_LEFT,
                    i * CELL_HEIGHT + BORDER_TOP + 1 + CELL_HEIGHT * 8.5f,
                    vu,
                    13);
            }
        }

        private void drawVoiceDetails(Graphics g)
        {
            const int CELL_HEIGHT = 16;
            const int BORDER_TOP = 2;
            const int BORDER_LEFT = 2;

            const int ENV_MODE_WIDTH = 32;
            const int ENV_VALUE_WIDTH = 32;
            const int SOURCE_INDEX_WIDTH = 32;
            const int RATE_WIDTH = 48;
            const int FX_WIDTH = 32;

            string? s = null;
            SizeF size;

            // erase the BG to black
            g.Clear(Color.Black);

            // display it like its done in SPC Tool
            for (int voiceIndex = 0; voiceIndex < 8; voiceIndex++)
            {
                // get the voice
                SPCVoice voice = _core.DSP.Voices(voiceIndex);

                // draw the voice's envelope mode
                if (voice.EnableADSR)
                {
                    s = "ADSR";
                }
                else
                {
                    switch (voice.GainMode)
                    {
                        case GainMode.Direct:
                            s = "Direct";
                            break;

                        case GainMode.ExponentialDecrease:
                            s = "Exp";
                            break;

                        case GainMode.LinearDecrease:
                            s = "Dec";
                            break;

                        case GainMode.LinearIncrease:
                            s = "Inc";
                            break;

                        case GainMode.BentIncrease:
                            s = "Bent";
                            break;
                    }
                }
                size = g.MeasureString(s, fontGeneralUI);
                g.DrawString(
                    s,
                    fontGeneralUI,
                    Brushes.LightGray,
                    BORDER_LEFT,
                    (CELL_HEIGHT - size.Height) / 2 + CELL_HEIGHT * voiceIndex + BORDER_TOP);

                // draw the voice's current envelope value (upper 7 bits of the envelope)
                s = string.Format("{0}", (voice.Envelope >> 4));
                size = g.MeasureString(s, fontGeneralUI);
                g.DrawString(
                    s,
                    fontGeneralUI,
                    Brushes.LightGray,
                    BORDER_LEFT + ENV_MODE_WIDTH + (ENV_VALUE_WIDTH - size.Width),
                    (CELL_HEIGHT - size.Height) / 2 + CELL_HEIGHT * voiceIndex + BORDER_TOP);

                // draw the voice's Source Index
                s = string.Format("{0}", voice.SourceIndex);
                size = g.MeasureString(s, fontGeneralUI);
                g.DrawString(
                    s,
                    fontGeneralUI,
                    Brushes.LightGray,
                    BORDER_LEFT + ENV_MODE_WIDTH + ENV_VALUE_WIDTH + (SOURCE_INDEX_WIDTH - size.Width),
                    (CELL_HEIGHT - size.Height) / 2 + CELL_HEIGHT * voiceIndex + BORDER_TOP);

                // draw the voice's playback rate
                s = string.Format("{0}", voice.Rate);
                size = g.MeasureString(s, fontGeneralUI);
                g.DrawString(
                    s,
                    fontGeneralUI,
                    Brushes.LightGray,
                    BORDER_LEFT + ENV_MODE_WIDTH + ENV_VALUE_WIDTH + SOURCE_INDEX_WIDTH + (RATE_WIDTH - size.Width),
                    (CELL_HEIGHT - size.Height) / 2 + CELL_HEIGHT * voiceIndex + BORDER_TOP);

                // draw the voice's FX flags
                // EPN (echo, pitch modulation, noise)
                s = string.Format("{0}{1}{2}",
                        voice.EchoEnabled ? "E" : " ",
                        voice.PitchModEnabled ? "P" : " ",
                        voice.NoiseEnabled ? "N" : " ");
                size = g.MeasureString(s, fontDspReg);
                g.DrawString(
                    s,
                    fontDspReg,
                    Brushes.LightGray,
                    BORDER_LEFT + ENV_MODE_WIDTH + ENV_VALUE_WIDTH + SOURCE_INDEX_WIDTH + RATE_WIDTH + FX_WIDTH * 0.125f,
                    (CELL_HEIGHT - size.Height) + CELL_HEIGHT * voiceIndex + BORDER_TOP);
            }
        }

        // need to invoke a thread-safe method here; atm I do not understand exactly why, but this works
        private void player_PlaybackStoppedSafe()
        {
            if (btnOpen.InvokeRequired)
            {
                btnOpen.BeginInvoke(new Action(() => { player_PlaybackStoppedSafe(); }));
            }
            else
            {
                btnOpen.Enabled = true;
                btnPlay.Enabled = true;
                btnPause.Enabled = false;
                btnStop.Enabled = false;
            }
        }

        private void _player_PlaybackStopped(object? sender, EventArgs e)
        {
            player_PlaybackStoppedSafe();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            loadSPC();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            playSPC();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            pauseSPC();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopSPC();
        }

        private bool[] mutes = { false, false, false, false, false, false, false, false };

        //private void btnMute_Click(object sender, EventArgs e)
        //{
        //    // get the channel number to toggle
        //    int index;
        //    if (sender == btnMute0) index = 0;
        //    else if (sender == btnMute1) index = 1;
        //    else if (sender == btnMute2) index = 2;
        //    else if (sender == btnMute3) index = 3;
        //    else if (sender == btnMute4) index = 4;
        //    else if (sender == btnMute5) index = 5;
        //    else if (sender == btnMute6) index = 6;
        //    else index = 7;

        //    mutes[index] = !mutes[index];

        //    // change the button's color to reflect this
        //    Button? source = sender as Button;

        //    if (mutes[index])
        //    {
        //        source!.BackColor = Color.Lime;
        //    }
        //    else
        //    {
        //        source!.BackColor = SystemColors.Control;
        //    }

        //    _core.DSP.SetVoiceMuted(index, mutes[index]);
        //}

        private void picDspRegs_Paint(object sender, PaintEventArgs e)
        {
            drawDspRegisters(e.Graphics);
        }

        private void picVoiceRawVolume_Paint(object sender, PaintEventArgs e)
        {
            drawVoiceVolumesRaw(e.Graphics);
        }

        private void picVoiceMeters_Paint(object sender, PaintEventArgs e)
        {
            drawVoiceMeters(e.Graphics);
        }

        private void picVoiceDetails_Paint(object sender, PaintEventArgs e)
        {
            drawVoiceDetails(e.Graphics);
        }

        private void tmrUpdateVisuals_Tick(object sender, EventArgs e)
        {
            // read the DSP registers and update their values and timers as needed
            for (int i = 0; i < 128; i++)
            {
                byte b = _core.DSP.GetRegisterDirect(i);

                if (_dspRegs[i] != b)
                {
                    // register value changed
                    _dspRegs[i] = b;
                    _dspRegTimer[i] = DSP_REG_TIMER_LOAD;
                }
                else if (_dspRegTimer[i] > 0) _dspRegTimer[i]--;
            }

            // update the visuals
            picDspRegs.Invalidate();
            picVoiceRawVolume.Invalidate();
            picVoiceMeters.Invalidate();
            picVoiceDetails.Invalidate();

        }

        private void FormPlayer_FormClosed(object sender, FormClosedEventArgs e)
        {
            stopSPC();
        }
    }
}