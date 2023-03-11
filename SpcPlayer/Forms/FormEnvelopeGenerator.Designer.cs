namespace SpcPlayer.Forms
{
    partial class FormEnvelopeGenerator
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtTicksPerSecond = new System.Windows.Forms.TextBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtSustainRate = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSustainLevel = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDecayRate = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtAttackRate = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.txtTicksPerSecond);
            this.groupBox1.Controls.Add(this.btnGenerate);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtSustainRate);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtSustainLevel);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtDecayRate);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtAttackRate);
            this.groupBox1.Location = new System.Drawing.Point(8, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(240, 96);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Parameters";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(8, 60);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 20);
            this.label5.TabIndex = 10;
            this.label5.Text = "Ticks per Second";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtTicksPerSecond
            // 
            this.txtTicksPerSecond.Location = new System.Drawing.Point(104, 56);
            this.txtTicksPerSecond.MaxLength = 4;
            this.txtTicksPerSecond.Name = "txtTicksPerSecond";
            this.txtTicksPerSecond.Size = new System.Drawing.Size(40, 23);
            this.txtTicksPerSecond.TabIndex = 9;
            this.txtTicksPerSecond.Text = "96";
            this.txtTicksPerSecond.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtTicksPerSecond.TextChanged += new System.EventHandler(this.txtTicksPerSecond_TextChanged);
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(152, 56);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(80, 32);
            this.btnGenerate.TabIndex = 8;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(176, 27);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(24, 16);
            this.label4.TabIndex = 7;
            this.label4.Text = "SR";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtSustainRate
            // 
            this.txtSustainRate.Location = new System.Drawing.Point(200, 24);
            this.txtSustainRate.MaxLength = 4;
            this.txtSustainRate.Name = "txtSustainRate";
            this.txtSustainRate.Size = new System.Drawing.Size(32, 23);
            this.txtSustainRate.TabIndex = 6;
            this.txtSustainRate.Text = "1A";
            this.txtSustainRate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtSustainRate.TextChanged += new System.EventHandler(this.txtSustainRate_TextChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(120, 27);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(24, 16);
            this.label3.TabIndex = 5;
            this.label3.Text = "SL";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtSustainLevel
            // 
            this.txtSustainLevel.Location = new System.Drawing.Point(144, 24);
            this.txtSustainLevel.MaxLength = 4;
            this.txtSustainLevel.Name = "txtSustainLevel";
            this.txtSustainLevel.Size = new System.Drawing.Size(24, 23);
            this.txtSustainLevel.TabIndex = 4;
            this.txtSustainLevel.Text = "7";
            this.txtSustainLevel.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtSustainLevel.TextChanged += new System.EventHandler(this.txtSustainLevel_TextChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(64, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(24, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "DR";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtDecayRate
            // 
            this.txtDecayRate.Location = new System.Drawing.Point(88, 24);
            this.txtDecayRate.MaxLength = 4;
            this.txtDecayRate.Name = "txtDecayRate";
            this.txtDecayRate.Size = new System.Drawing.Size(24, 23);
            this.txtDecayRate.TabIndex = 2;
            this.txtDecayRate.Text = "0";
            this.txtDecayRate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtDecayRate.TextChanged += new System.EventHandler(this.txtDecayRate_TextChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "AR";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtAttackRate
            // 
            this.txtAttackRate.Location = new System.Drawing.Point(32, 24);
            this.txtAttackRate.MaxLength = 4;
            this.txtAttackRate.Name = "txtAttackRate";
            this.txtAttackRate.Size = new System.Drawing.Size(24, 23);
            this.txtAttackRate.TabIndex = 0;
            this.txtAttackRate.Text = "F";
            this.txtAttackRate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtAttackRate.TextChanged += new System.EventHandler(this.txtAttackRate_TextChanged);
            // 
            // FormEnvelopeGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormEnvelopeGenerator";
            this.Text = "Envelope Generator";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private GroupBox groupBox1;
        private Label label4;
        private TextBox txtSustainRate;
        private Label label3;
        private TextBox txtSustainLevel;
        private Label label2;
        private TextBox txtDecayRate;
        private Label label1;
        private TextBox txtAttackRate;
        private Button btnGenerate;
        private Label label5;
        private TextBox txtTicksPerSecond;
    }
}