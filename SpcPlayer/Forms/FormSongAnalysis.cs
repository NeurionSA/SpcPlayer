using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpcPlayer.SPC;
using SpcPlayer.SPC.Analysis;
using SpcPlayer.SPC.Logging;

namespace SpcPlayer.Forms
{
    public partial class FormSongAnalysis : Form
    {
        // colored brushes and pens used for rendering the timeline with blocks colored by channel number
        private static SolidBrush[] timelineChannelBrushes = {
            new SolidBrush(Color.FromArgb(255, 0, 0)),
            new SolidBrush(Color.FromArgb(255, 128, 0)),
            new SolidBrush(Color.FromArgb(255, 255, 0)),
            new SolidBrush(Color.FromArgb(57, 255, 0)),
            new SolidBrush(Color.FromArgb(0, 255, 230)),
            new SolidBrush(Color.FromArgb(0, 108, 255)),
            new SolidBrush(Color.FromArgb(198, 0, 255)),
            new SolidBrush(Color.FromArgb(255, 0, 96)),
        };
        private static Pen[] timelineChannelPens = {
            new Pen(Color.FromArgb(255, 168, 168)),
            new Pen(Color.FromArgb(255, 213, 170)),
            new Pen(Color.FromArgb(255, 255, 170)),
            new Pen(Color.FromArgb(189, 255, 170)),
            new Pen(Color.FromArgb(170, 255, 247)),
            new Pen(Color.FromArgb(170, 206, 255)),
            new Pen(Color.FromArgb(236, 170, 255)),
            new Pen(Color.FromArgb(255, 170, 202)),
        };


        // constants for the rendering of the song timeline
        private const int TIMELINE_BORDER_LEFT = 8;
        private const int TIMELINE_CELL_WIDTH = 64;
        private const int TIMELINE_CELL_SPACING = 8;
        private const int TIMELINE_RENDER_HEIGHT = 512;

        // the SPC core
        private SPCCore _core = new SPCCore(32000);
        // the loaded SPC file
        private SPCFile? _spcFile;
        private SongAnalysis? _analysis;

        // the bitmap the song timeline is rendered to
        private Bitmap _bmpTimeline;

        private bool _fileLoaded = false;

        private int _firstSample = 0;

        // scaling settings for the analysis view
        private float _samplesPerPixel = 250;  // the number of audio samples per vertical pixel
        private int _maxFirstSecond;

        public FormSongAnalysis()
        {
            InitializeComponent();

            // add mouse wheel handler for analysis pic
            picSongTimeline.MouseWheel += picAnalysis_MouseWheel;

            // create the render bitmap using the sizes defined by the constants
            _bmpTimeline = new Bitmap(TIMELINE_BORDER_LEFT + (TIMELINE_CELL_WIDTH + TIMELINE_CELL_SPACING) * 8, TIMELINE_RENDER_HEIGHT);

            // enable heavy logging in the DSP
            _core.DSP.EnableHeavyLogging = true;
        }

        private void renderTimeline()
        {
            Graphics g = Graphics.FromImage(_bmpTimeline);

            g.Clear(Color.Black);

            // calculate the maximum sample number for the render area
            int maxSampleNumber = (int)(TIMELINE_RENDER_HEIGHT * _samplesPerPixel) + _firstSample;

            // find the index of the first Note that's active during the time of the first sample
            int firstNoteIndex = _analysis!.GetIndexOfFirstNoteAtTime(_firstSample);
            
            for (int i = firstNoteIndex; i < _analysis.NoteCount; i++)
            {
                Note note = _analysis.Notes(i);

                // if the note starts outside of the render area then break out of the loop
                if (note.StartTime >= maxSampleNumber) break;

                // draw the note's rectangle
                float x = TIMELINE_BORDER_LEFT + (TIMELINE_CELL_WIDTH + TIMELINE_CELL_SPACING) * note.Channel;
                float y = (note.StartTime - _firstSample) / _samplesPerPixel;
                float width = TIMELINE_CELL_WIDTH;
                float height = note.Duration / _samplesPerPixel;

                g.FillRectangle(timelineChannelBrushes[note.Channel], x, y, width, height);
                g.DrawRectangle(timelineChannelPens[note.Channel], x, y, width, height);
            }

            g.Dispose();
        }

        private void onEmulationSuccess()
        {
            // enable or disable UI elements based on if the file load was successful
            if (_fileLoaded)
            {
                vsbSongTimeline.Enabled = true;
            }
            else
            {
                vsbSongTimeline.Enabled = false;
            }
        }

        private void loadSPC()
        {
            // show the file open dlg
            DialogResult result = odlgOpenSpc.ShowDialog();
            // if a file wasn't selected the nexit
            if (result != DialogResult.OK) return;

            // load the SPC file
            _spcFile = new SPCFile(odlgOpenSpc.FileName);

            // load the file into the core
            _core.LoadStateFromFile(_spcFile);

            FormEmulatePlaybackDialog formEPD = new FormEmulatePlaybackDialog(_core, _spcFile.SecondsBeforeFade);

            result = formEPD.ShowDialog();

            if (result != DialogResult.OK) return;

            _fileLoaded = true;
            vsbSongTimeline.Enabled = true;
            _firstSample = 0;
            vsbSongTimeline.Value = 0;
            // need to do some dumb stuff with the maximum we set here, otherwise the user interface will not allow
            // me to actually reach the maximum I define here
            // "The maximum value can only be reached programmatically. The value of a scroll bar cannot reach its
            // maximum value through user interaction at run time. The maximum value that can be reached through
            // user interaction is equal to 1 plus the Maximum property value minus the LargeChange property value."
            _maxFirstSecond = _spcFile.SecondsBeforeFade - 1;
            vsbSongTimeline.Maximum = _maxFirstSecond + vsbSongTimeline.LargeChange - 1;

            // create the song analysis from the DSP's current state
            _analysis = new SongAnalysis(_core.DSP);

            renderTimeline();
            picSongTimeline.Invalidate();

            Cursor = Cursors.Default;
        }

        private void rebaseAnalysisView()
        {
            _firstSample = 32000 * vsbSongTimeline.Value;
            renderTimeline();
            picSongTimeline.Invalidate();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            loadSPC();
        }

        private void picAnalysis_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            if (_fileLoaded) e.Graphics.DrawImage(_bmpTimeline, 0, 0);
        }

        private void picAnalysis_MouseWheel(object? sender, MouseEventArgs e)
        {
            // only proceed if a file has been loaded
            if (!_fileLoaded) return;

            int oldValue = vsbSongTimeline.Value;

            if (e.Delta > 0)
            {
                // scroll up
                vsbSongTimeline.Value = vsbSongTimeline.Value - 1 > 0 ? vsbSongTimeline.Value - 1 : 0;
            }
            else if (e.Delta < 0)
            {
                // scroll down
                vsbSongTimeline.Value = vsbSongTimeline.Value + 1 < _maxFirstSecond ? vsbSongTimeline.Value + 1 : _maxFirstSecond;
            }
        }

        private void vsbRender_ValueChanged(object sender, EventArgs e)
        {
            rebaseAnalysisView();
        }
    }
}
