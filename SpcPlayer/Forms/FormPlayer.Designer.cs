namespace SpcPlayer
{
    partial class FormPlayer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.odlgOpenSpc = new System.Windows.Forms.OpenFileDialog();
            this.picDspRegs = new System.Windows.Forms.PictureBox();
            this.tmrUpdateVisuals = new System.Windows.Forms.Timer(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.picVoiceDetails = new System.Windows.Forms.PictureBox();
            this.picVoiceMeters = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.picVoiceRawVolume = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picDspRegs)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picVoiceDetails)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picVoiceMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picVoiceRawVolume)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(8, 8);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(72, 40);
            this.btnOpen.TabIndex = 0;
            this.btnOpen.Text = "Open...";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Enabled = false;
            this.btnPlay.Location = new System.Drawing.Point(88, 8);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(72, 40);
            this.btnPlay.TabIndex = 1;
            this.btnPlay.Text = "Play";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnPause
            // 
            this.btnPause.Enabled = false;
            this.btnPause.Location = new System.Drawing.Point(168, 8);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(72, 40);
            this.btnPause.TabIndex = 2;
            this.btnPause.Text = "Pause";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(248, 8);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(72, 40);
            this.btnStop.TabIndex = 3;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // odlgOpenSpc
            // 
            this.odlgOpenSpc.Filter = "SPC Files|*.spc|All files|*.*";
            this.odlgOpenSpc.Title = "Open SPC";
            // 
            // picDspRegs
            // 
            this.picDspRegs.BackColor = System.Drawing.Color.Black;
            this.picDspRegs.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picDspRegs.Location = new System.Drawing.Point(8, 16);
            this.picDspRegs.Name = "picDspRegs";
            this.picDspRegs.Size = new System.Drawing.Size(188, 280);
            this.picDspRegs.TabIndex = 12;
            this.picDspRegs.TabStop = false;
            this.picDspRegs.Paint += new System.Windows.Forms.PaintEventHandler(this.picDspRegs_Paint);
            // 
            // tmrUpdateVisuals
            // 
            this.tmrUpdateVisuals.Interval = 33;
            this.tmrUpdateVisuals.Tick += new System.EventHandler(this.tmrUpdateVisuals_Tick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.picDspRegs);
            this.groupBox1.Location = new System.Drawing.Point(504, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(204, 304);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "DSP Registers";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.picVoiceDetails);
            this.groupBox2.Controls.Add(this.picVoiceMeters);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.picVoiceRawVolume);
            this.groupBox2.Location = new System.Drawing.Point(8, 56);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(488, 216);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Voices";
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(448, 16);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(32, 16);
            this.label7.TabIndex = 18;
            this.label7.Text = "FX";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(400, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 16);
            this.label6.TabIndex = 17;
            this.label6.Text = "Rate";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(368, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 16);
            this.label5.TabIndex = 16;
            this.label5.Text = "Src.";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(336, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 16);
            this.label4.TabIndex = 15;
            this.label4.Text = "Env.";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(296, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 16);
            this.label3.TabIndex = 5;
            this.label3.Text = "Mode";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // picVoiceDetails
            // 
            this.picVoiceDetails.BackColor = System.Drawing.Color.Black;
            this.picVoiceDetails.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picVoiceDetails.Location = new System.Drawing.Point(296, 32);
            this.picVoiceDetails.Name = "picVoiceDetails";
            this.picVoiceDetails.Size = new System.Drawing.Size(184, 176);
            this.picVoiceDetails.TabIndex = 4;
            this.picVoiceDetails.TabStop = false;
            this.picVoiceDetails.Paint += new System.Windows.Forms.PaintEventHandler(this.picVoiceDetails_Paint);
            // 
            // picVoiceMeters
            // 
            this.picVoiceMeters.BackColor = System.Drawing.Color.Black;
            this.picVoiceMeters.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picVoiceMeters.Location = new System.Drawing.Point(80, 32);
            this.picVoiceMeters.Name = "picVoiceMeters";
            this.picVoiceMeters.Size = new System.Drawing.Size(208, 176);
            this.picVoiceMeters.TabIndex = 3;
            this.picVoiceMeters.TabStop = false;
            this.picVoiceMeters.Paint += new System.Windows.Forms.PaintEventHandler(this.picVoiceMeters_Paint);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(40, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "R";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "L";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // picVoiceRawVolume
            // 
            this.picVoiceRawVolume.BackColor = System.Drawing.Color.Black;
            this.picVoiceRawVolume.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picVoiceRawVolume.Location = new System.Drawing.Point(8, 32);
            this.picVoiceRawVolume.Name = "picVoiceRawVolume";
            this.picVoiceRawVolume.Size = new System.Drawing.Size(64, 176);
            this.picVoiceRawVolume.TabIndex = 0;
            this.picVoiceRawVolume.TabStop = false;
            this.picVoiceRawVolume.Paint += new System.Windows.Forms.PaintEventHandler(this.picVoiceRawVolume_Paint);
            // 
            // FormPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.btnOpen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormPlayer";
            this.Text = "SPC Player";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormPlayer_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.picDspRegs)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picVoiceDetails)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picVoiceMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picVoiceRawVolume)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Button btnOpen;
        private Button btnPlay;
        private Button btnPause;
        private Button btnStop;
        private OpenFileDialog odlgOpenSpc;
        private PictureBox picDspRegs;
        private System.Windows.Forms.Timer tmrUpdateVisuals;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private PictureBox picVoiceRawVolume;
        private Label label2;
        private Label label1;
        private PictureBox picVoiceMeters;
        private PictureBox picVoiceDetails;
        private Label label3;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
    }
}