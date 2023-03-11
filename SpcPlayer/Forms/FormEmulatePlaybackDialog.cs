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

namespace SpcPlayer.Forms
{
    internal partial class FormEmulatePlaybackDialog : Form
    {
        private SPCCore _core;
        private int _secondsToEmulate;

        public FormEmulatePlaybackDialog(SPCCore core, int seconds)
        {
            InitializeComponent();
            ArgumentNullException.ThrowIfNull(core);

            _core = core;
            _secondsToEmulate = seconds;

            lblPlayback.Text = string.Format("Emulating {0} seconds of playback...", seconds);
            pbProgress.Value = 0;
        }

        private void bgwEmulate_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < _secondsToEmulate; i++)
            {
                if (bgwEmulate.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // run 1 second worth of SPC CPU cycles
                _core.ExecuteCycles(1024000);
                // empty the DSP output buffer, it's not needed
                _core.DSP.RetrieveSamples();
                // report progress
                bgwEmulate.ReportProgress((int)(i / (float)_secondsToEmulate * 100));
                Thread.Sleep(0);
            }

            e.Result = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (bgwEmulate.WorkerSupportsCancellation)
            {
                bgwEmulate.CancelAsync();
            }
        }

        private void FormEmulatePlaybackDialog_Shown(object sender, EventArgs e)
        {
            if (!bgwEmulate.IsBusy)
            {
                bgwEmulate.RunWorkerAsync();
            }
        }

        private void bgwEmulate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // there was an error
                MessageBox.Show(e.Error.Message);
                DialogResult = DialogResult.Cancel;
            }
            else if (e.Cancelled)
            {
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                // execution was successful
                DialogResult = DialogResult.OK;
            }

            // close the form
            Close();
        }

        private void bgwEmulate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbProgress.Value = e.ProgressPercentage;
        }

        private void FormEmulatePlaybackDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // if the user is trying to close the form AND the worker is busy
            if (e.CloseReason == CloseReason.UserClosing && bgwEmulate.IsBusy)
            {
                // this counts as hitting the cancel button
                if (bgwEmulate.WorkerSupportsCancellation)
                {
                    bgwEmulate.CancelAsync();
                    e.Cancel = true;
                }
            }
        }
    }
}
