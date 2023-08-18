namespace PopGen2
{
	partial class frm_PopGen
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
			this.components = new System.ComponentModel.Container();
			this.lbl_Girls = new System.Windows.Forms.Label();
			this.lbl_Boys = new System.Windows.Forms.Label();
			this.lbl_Ladies_Available = new System.Windows.Forms.Label();
			this.lbl_Wives = new System.Windows.Forms.Label();
			this.lbl_Men = new System.Windows.Forms.Label();
			this.lbl_Widows_PCBA = new System.Windows.Forms.Label();
			this.lbl_Total_Pop = new System.Windows.Forms.Label();
			this.tmr_Iterate = new System.Windows.Forms.Timer(this.components);
			this.lbl_Date = new System.Windows.Forms.Label();
			this.cbx_Diag_Trace = new System.Windows.Forms.CheckBox();
			this.ProgBar = new System.Windows.Forms.ProgressBar();
			this.btn_Publish = new System.Windows.Forms.Button();
			this.lbl_Progress = new System.Windows.Forms.Label();
			this.cbx_Slow_Step = new System.Windows.Forms.CheckBox();
			this.btn_Pause_Resume = new System.Windows.Forms.Button();
			this.txb_Timer_MS = new System.Windows.Forms.TextBox();
			this.lbl_MS = new System.Windows.Forms.Label();
			this.btn_Pause_After_Run = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// lbl_Girls
			// 
			this.lbl_Girls.AutoSize = true;
			this.lbl_Girls.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Girls.Location = new System.Drawing.Point(12, 32);
			this.lbl_Girls.Name = "lbl_Girls";
			this.lbl_Girls.Size = new System.Drawing.Size(16, 16);
			this.lbl_Girls.TabIndex = 0;
			this.lbl_Girls.Text = "0";
			// 
			// lbl_Boys
			// 
			this.lbl_Boys.AutoSize = true;
			this.lbl_Boys.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Boys.Location = new System.Drawing.Point(12, 49);
			this.lbl_Boys.Name = "lbl_Boys";
			this.lbl_Boys.Size = new System.Drawing.Size(16, 16);
			this.lbl_Boys.TabIndex = 1;
			this.lbl_Boys.Text = "0";
			// 
			// lbl_Ladies_Available
			// 
			this.lbl_Ladies_Available.AutoSize = true;
			this.lbl_Ladies_Available.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Ladies_Available.Location = new System.Drawing.Point(12, 66);
			this.lbl_Ladies_Available.Name = "lbl_Ladies_Available";
			this.lbl_Ladies_Available.Size = new System.Drawing.Size(16, 16);
			this.lbl_Ladies_Available.TabIndex = 2;
			this.lbl_Ladies_Available.Text = "0";
			// 
			// lbl_Wives
			// 
			this.lbl_Wives.AutoSize = true;
			this.lbl_Wives.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Wives.Location = new System.Drawing.Point(12, 83);
			this.lbl_Wives.Name = "lbl_Wives";
			this.lbl_Wives.Size = new System.Drawing.Size(16, 16);
			this.lbl_Wives.TabIndex = 3;
			this.lbl_Wives.Text = "0";
			// 
			// lbl_Men
			// 
			this.lbl_Men.AutoSize = true;
			this.lbl_Men.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Men.Location = new System.Drawing.Point(12, 100);
			this.lbl_Men.Name = "lbl_Men";
			this.lbl_Men.Size = new System.Drawing.Size(16, 16);
			this.lbl_Men.TabIndex = 4;
			this.lbl_Men.Text = "0";
			// 
			// lbl_Widows_PCBA
			// 
			this.lbl_Widows_PCBA.AutoSize = true;
			this.lbl_Widows_PCBA.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Widows_PCBA.Location = new System.Drawing.Point(12, 117);
			this.lbl_Widows_PCBA.Name = "lbl_Widows_PCBA";
			this.lbl_Widows_PCBA.Size = new System.Drawing.Size(16, 16);
			this.lbl_Widows_PCBA.TabIndex = 5;
			this.lbl_Widows_PCBA.Text = "0";
			// 
			// lbl_Total_Pop
			// 
			this.lbl_Total_Pop.AutoSize = true;
			this.lbl_Total_Pop.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Total_Pop.Location = new System.Drawing.Point(12, 134);
			this.lbl_Total_Pop.Name = "lbl_Total_Pop";
			this.lbl_Total_Pop.Size = new System.Drawing.Size(16, 16);
			this.lbl_Total_Pop.TabIndex = 6;
			this.lbl_Total_Pop.Text = "0";
			// 
			// tmr_Iterate
			// 
			this.tmr_Iterate.Interval = 1;
			this.tmr_Iterate.Tick += new System.EventHandler(this.tmr_Iterate_Tick);
			// 
			// lbl_Date
			// 
			this.lbl_Date.AutoSize = true;
			this.lbl_Date.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Date.Location = new System.Drawing.Point(12, 9);
			this.lbl_Date.Name = "lbl_Date";
			this.lbl_Date.Size = new System.Drawing.Size(16, 16);
			this.lbl_Date.TabIndex = 7;
			this.lbl_Date.Text = "0";
			// 
			// cbx_Diag_Trace
			// 
			this.cbx_Diag_Trace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cbx_Diag_Trace.AutoSize = true;
			this.cbx_Diag_Trace.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cbx_Diag_Trace.Location = new System.Drawing.Point(634, 135);
			this.cbx_Diag_Trace.Name = "cbx_Diag_Trace";
			this.cbx_Diag_Trace.Size = new System.Drawing.Size(108, 20);
			this.cbx_Diag_Trace.TabIndex = 8;
			this.cbx_Diag_Trace.Text = "Diags Trace";
			this.cbx_Diag_Trace.UseVisualStyleBackColor = true;
			this.cbx_Diag_Trace.CheckedChanged += new System.EventHandler(this.cbx_Diag_Trace_CheckedChanged);
			// 
			// ProgBar
			// 
			this.ProgBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
							| System.Windows.Forms.AnchorStyles.Right)));
			this.ProgBar.Location = new System.Drawing.Point(2, 157);
			this.ProgBar.Name = "ProgBar";
			this.ProgBar.Size = new System.Drawing.Size(769, 32);
			this.ProgBar.TabIndex = 9;
			// 
			// btn_Publish
			// 
			this.btn_Publish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_Publish.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btn_Publish.Location = new System.Drawing.Point(667, 9);
			this.btn_Publish.Name = "btn_Publish";
			this.btn_Publish.Size = new System.Drawing.Size(99, 43);
			this.btn_Publish.TabIndex = 10;
			this.btn_Publish.Text = "Re-Publish";
			this.btn_Publish.UseVisualStyleBackColor = true;
			this.btn_Publish.Click += new System.EventHandler(this.btn_Publish_Click);
			// 
			// lbl_Progress
			// 
			this.lbl_Progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
							| System.Windows.Forms.AnchorStyles.Right)));
			this.lbl_Progress.BackColor = System.Drawing.Color.Transparent;
			this.lbl_Progress.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_Progress.Location = new System.Drawing.Point(10, 166);
			this.lbl_Progress.Name = "lbl_Progress";
			this.lbl_Progress.Size = new System.Drawing.Size(752, 15);
			this.lbl_Progress.TabIndex = 11;
			this.lbl_Progress.Text = "0";
			this.lbl_Progress.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// cbx_Slow_Step
			// 
			this.cbx_Slow_Step.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cbx_Slow_Step.AutoSize = true;
			this.cbx_Slow_Step.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cbx_Slow_Step.Location = new System.Drawing.Point(634, 109);
			this.cbx_Slow_Step.Name = "cbx_Slow_Step";
			this.cbx_Slow_Step.Size = new System.Drawing.Size(96, 20);
			this.cbx_Slow_Step.TabIndex = 12;
			this.cbx_Slow_Step.Text = "Slow Step";
			this.cbx_Slow_Step.UseVisualStyleBackColor = true;
			this.cbx_Slow_Step.CheckedChanged += new System.EventHandler(this.ckb_Slow_Step_CheckedChanged);
			// 
			// btn_Pause_Resume
			// 
			this.btn_Pause_Resume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_Pause_Resume.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btn_Pause_Resume.Location = new System.Drawing.Point(562, 9);
			this.btn_Pause_Resume.Name = "btn_Pause_Resume";
			this.btn_Pause_Resume.Size = new System.Drawing.Size(99, 43);
			this.btn_Pause_Resume.TabIndex = 13;
			this.btn_Pause_Resume.Text = "Pause";
			this.btn_Pause_Resume.UseVisualStyleBackColor = true;
			this.btn_Pause_Resume.Click += new System.EventHandler(this.btn_Pause_Resume_Click);
			// 
			// txb_Timer_MS
			// 
			this.txb_Timer_MS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.txb_Timer_MS.Location = new System.Drawing.Point(634, 81);
			this.txb_Timer_MS.Name = "txb_Timer_MS";
			this.txb_Timer_MS.Size = new System.Drawing.Size(58, 22);
			this.txb_Timer_MS.TabIndex = 14;
			this.txb_Timer_MS.Text = "1";
			this.txb_Timer_MS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.txb_Timer_MS.TextChanged += new System.EventHandler(this.txb_Timer_MS_TextChanged);
			// 
			// lbl_MS
			// 
			this.lbl_MS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbl_MS.AutoSize = true;
			this.lbl_MS.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbl_MS.Location = new System.Drawing.Point(698, 83);
			this.lbl_MS.Name = "lbl_MS";
			this.lbl_MS.Size = new System.Drawing.Size(28, 16);
			this.lbl_MS.TabIndex = 15;
			this.lbl_MS.Text = "ms";
			// 
			// btn_Pause_After_Run
			// 
			this.btn_Pause_After_Run.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_Pause_After_Run.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.btn_Pause_After_Run.Location = new System.Drawing.Point(457, 9);
			this.btn_Pause_After_Run.Name = "btn_Pause_After_Run";
			this.btn_Pause_After_Run.Size = new System.Drawing.Size(99, 43);
			this.btn_Pause_After_Run.TabIndex = 16;
			this.btn_Pause_After_Run.Text = "Pause After Run";
			this.btn_Pause_After_Run.UseVisualStyleBackColor = true;
			this.btn_Pause_After_Run.Click += new System.EventHandler(this.btn_Pause_After_Run_Click);
			// 
			// frm_PopGen
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(772, 191);
			this.Controls.Add(this.btn_Pause_After_Run);
			this.Controls.Add(this.lbl_MS);
			this.Controls.Add(this.txb_Timer_MS);
			this.Controls.Add(this.btn_Pause_Resume);
			this.Controls.Add(this.cbx_Slow_Step);
			this.Controls.Add(this.lbl_Progress);
			this.Controls.Add(this.btn_Publish);
			this.Controls.Add(this.ProgBar);
			this.Controls.Add(this.cbx_Diag_Trace);
			this.Controls.Add(this.lbl_Date);
			this.Controls.Add(this.lbl_Total_Pop);
			this.Controls.Add(this.lbl_Widows_PCBA);
			this.Controls.Add(this.lbl_Men);
			this.Controls.Add(this.lbl_Wives);
			this.Controls.Add(this.lbl_Ladies_Available);
			this.Controls.Add(this.lbl_Boys);
			this.Controls.Add(this.lbl_Girls);
			this.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "frm_PopGen";
			this.Text = " Population Growth Model";
			this.Load += new System.EventHandler(this.frm_PopGen_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lbl_Girls;
		private System.Windows.Forms.Label lbl_Boys;
		private System.Windows.Forms.Label lbl_Ladies_Available;
		private System.Windows.Forms.Label lbl_Wives;
		private System.Windows.Forms.Label lbl_Men;
		private System.Windows.Forms.Label lbl_Widows_PCBA;
		private System.Windows.Forms.Label lbl_Total_Pop;
		private System.Windows.Forms.Timer tmr_Iterate;
		private System.Windows.Forms.Label lbl_Date;
		private System.Windows.Forms.CheckBox cbx_Diag_Trace;
		private System.Windows.Forms.ProgressBar ProgBar;
		private System.Windows.Forms.Button btn_Publish;
		private System.Windows.Forms.Label lbl_Progress;
		private System.Windows.Forms.CheckBox cbx_Slow_Step;
		private System.Windows.Forms.Button btn_Pause_Resume;
		private System.Windows.Forms.TextBox txb_Timer_MS;
		private System.Windows.Forms.Label lbl_MS;
		public System.Windows.Forms.Button btn_Pause_After_Run;
	}
}

