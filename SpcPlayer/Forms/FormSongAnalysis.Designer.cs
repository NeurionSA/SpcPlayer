namespace SpcPlayer.Forms
{
    partial class FormSongAnalysis
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
            this.odlgOpenSpc = new System.Windows.Forms.OpenFileDialog();
            this.btnOpen = new System.Windows.Forms.Button();
            this.picSongTimeline = new System.Windows.Forms.PictureBox();
            this.vsbSongTimeline = new System.Windows.Forms.VScrollBar();
            this.grpSongRender = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.picSongTimeline)).BeginInit();
            this.grpSongRender.SuspendLayout();
            this.SuspendLayout();
            // 
            // odlgOpenSpc
            // 
            this.odlgOpenSpc.FileName = "openFileDialog1";
            this.odlgOpenSpc.Filter = "SPC Files|*.spc|All files|*.*";
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(8, 8);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(72, 32);
            this.btnOpen.TabIndex = 0;
            this.btnOpen.Text = "Open...";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // picSongTimeline
            // 
            this.picSongTimeline.Location = new System.Drawing.Point(8, 24);
            this.picSongTimeline.Name = "picSongTimeline";
            this.picSongTimeline.Size = new System.Drawing.Size(584, 392);
            this.picSongTimeline.TabIndex = 1;
            this.picSongTimeline.TabStop = false;
            this.picSongTimeline.Paint += new System.Windows.Forms.PaintEventHandler(this.picAnalysis_Paint);
            // 
            // vsbSongTimeline
            // 
            this.vsbSongTimeline.Enabled = false;
            this.vsbSongTimeline.Location = new System.Drawing.Point(592, 24);
            this.vsbSongTimeline.Name = "vsbSongTimeline";
            this.vsbSongTimeline.Size = new System.Drawing.Size(16, 392);
            this.vsbSongTimeline.TabIndex = 2;
            this.vsbSongTimeline.ValueChanged += new System.EventHandler(this.vsbRender_ValueChanged);
            // 
            // grpSongRender
            // 
            this.grpSongRender.Controls.Add(this.picSongTimeline);
            this.grpSongRender.Controls.Add(this.vsbSongTimeline);
            this.grpSongRender.Location = new System.Drawing.Point(8, 48);
            this.grpSongRender.Name = "grpSongRender";
            this.grpSongRender.Size = new System.Drawing.Size(616, 424);
            this.grpSongRender.TabIndex = 3;
            this.grpSongRender.TabStop = false;
            this.grpSongRender.Text = "Song Timeline";
            // 
            // FormSongAnalysis
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 492);
            this.Controls.Add(this.grpSongRender);
            this.Controls.Add(this.btnOpen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormSongAnalysis";
            this.Text = "FormSongAnalysis";
            ((System.ComponentModel.ISupportInitialize)(this.picSongTimeline)).EndInit();
            this.grpSongRender.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenFileDialog odlgOpenSpc;
        private Button btnOpen;
        private PictureBox picSongTimeline;
        private VScrollBar vsbSongTimeline;
        private GroupBox grpSongRender;
    }
}