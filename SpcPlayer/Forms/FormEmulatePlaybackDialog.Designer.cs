namespace SpcPlayer.Forms
{
    partial class FormEmulatePlaybackDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblPlayback = new System.Windows.Forms.Label();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.btnCancel = new System.Windows.Forms.Button();
            this.bgwEmulate = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // lblPlayback
            // 
            this.lblPlayback.AutoSize = true;
            this.lblPlayback.Location = new System.Drawing.Point(8, 8);
            this.lblPlayback.Name = "lblPlayback";
            this.lblPlayback.Size = new System.Drawing.Size(197, 15);
            this.lblPlayback.TabIndex = 0;
            this.lblPlayback.Text = "Emulating {0} seconds of playback...";
            // 
            // pbProgress
            // 
            this.pbProgress.Location = new System.Drawing.Point(8, 32);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(232, 16);
            this.pbProgress.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(168, 56);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(72, 32);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // bgwEmulate
            // 
            this.bgwEmulate.WorkerReportsProgress = true;
            this.bgwEmulate.WorkerSupportsCancellation = true;
            this.bgwEmulate.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgwEmulate_DoWork);
            this.bgwEmulate.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.bgwEmulate_ProgressChanged);
            this.bgwEmulate.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bgwEmulate_RunWorkerCompleted);
            // 
            // FormEmulatePlaybackDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(248, 96);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.pbProgress);
            this.Controls.Add(this.lblPlayback);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormEmulatePlaybackDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Emulating Playback";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormEmulatePlaybackDialog_FormClosing);
            this.Shown += new System.EventHandler(this.FormEmulatePlaybackDialog_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblPlayback;
        private ProgressBar pbProgress;
        private Button btnCancel;
        private System.ComponentModel.BackgroundWorker bgwEmulate;
    }
}