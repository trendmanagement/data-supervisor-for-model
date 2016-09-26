namespace DataSupervisorForModel
{
    partial class RealtimeDataManagement
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RealtimeDataManagement));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.statusStripOptionMonitor = new System.Windows.Forms.StatusStrip();
            this.ConnectionStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusSubscribeData = new System.Windows.Forms.ToolStripStatusLabel();
            this.DataStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnCallAllInstruments = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
            this.btnCallUnsubscribed = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
            this.btnResetAllInstruments = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
            this.btnCQGRecon = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripSeparator();
            this.mainRealtimeMenuStrip = new System.Windows.Forms.MenuStrip();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.expressionListDataGrid = new System.Windows.Forms.DataGridView();
            this.statusStripOptionMonitor.SuspendLayout();
            this.mainRealtimeMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.expressionListDataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStripOptionMonitor
            // 
            this.statusStripOptionMonitor.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusStripOptionMonitor.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConnectionStatus,
            this.StatusSubscribeData,
            this.DataStatus});
            this.statusStripOptionMonitor.Location = new System.Drawing.Point(0, 391);
            this.statusStripOptionMonitor.Name = "statusStripOptionMonitor";
            this.statusStripOptionMonitor.Size = new System.Drawing.Size(795, 22);
            this.statusStripOptionMonitor.TabIndex = 6;
            this.statusStripOptionMonitor.Text = "statusStrip1";
            // 
            // ConnectionStatus
            // 
            this.ConnectionStatus.BackColor = System.Drawing.Color.Black;
            this.ConnectionStatus.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConnectionStatus.ForeColor = System.Drawing.Color.White;
            this.ConnectionStatus.Image = ((System.Drawing.Image)(resources.GetObject("ConnectionStatus.Image")));
            this.ConnectionStatus.Name = "ConnectionStatus";
            this.ConnectionStatus.Padding = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this.ConnectionStatus.Size = new System.Drawing.Size(48, 17);
            this.ConnectionStatus.Text = "_";
            // 
            // StatusSubscribeData
            // 
            this.StatusSubscribeData.BackColor = System.Drawing.Color.White;
            this.StatusSubscribeData.Name = "StatusSubscribeData";
            this.StatusSubscribeData.Size = new System.Drawing.Size(13, 17);
            this.StatusSubscribeData.Text = "_";
            // 
            // DataStatus
            // 
            this.DataStatus.BackColor = System.Drawing.Color.Black;
            this.DataStatus.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DataStatus.ForeColor = System.Drawing.Color.White;
            this.DataStatus.Name = "DataStatus";
            this.DataStatus.Size = new System.Drawing.Size(12, 17);
            this.DataStatus.Text = "_";
            // 
            // btnCallAllInstruments
            // 
            this.btnCallAllInstruments.BackColor = System.Drawing.Color.GreenYellow;
            this.btnCallAllInstruments.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnCallAllInstruments.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCallAllInstruments.Image = ((System.Drawing.Image)(resources.GetObject("btnCallAllInstruments.Image")));
            this.btnCallAllInstruments.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCallAllInstruments.Name = "btnCallAllInstruments";
            this.btnCallAllInstruments.Size = new System.Drawing.Size(46, 20);
            this.btnCallAllInstruments.Text = "Call All";
            this.btnCallAllInstruments.Click += new System.EventHandler(this.btnCallAllInstruments_Click);
            // 
            // toolStripSeparator17
            // 
            this.toolStripSeparator17.Name = "toolStripSeparator17";
            this.toolStripSeparator17.Size = new System.Drawing.Size(6, 23);
            // 
            // btnCallUnsubscribed
            // 
            this.btnCallUnsubscribed.BackColor = System.Drawing.Color.Yellow;
            this.btnCallUnsubscribed.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnCallUnsubscribed.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCallUnsubscribed.Image = ((System.Drawing.Image)(resources.GetObject("btnCallUnsubscribed.Image")));
            this.btnCallUnsubscribed.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCallUnsubscribed.Name = "btnCallUnsubscribed";
            this.btnCallUnsubscribed.Size = new System.Drawing.Size(70, 20);
            this.btnCallUnsubscribed.Text = "Call Unsub.";
            // 
            // toolStripSeparator18
            // 
            this.toolStripSeparator18.Name = "toolStripSeparator18";
            this.toolStripSeparator18.Size = new System.Drawing.Size(6, 23);
            // 
            // btnResetAllInstruments
            // 
            this.btnResetAllInstruments.BackColor = System.Drawing.Color.Red;
            this.btnResetAllInstruments.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnResetAllInstruments.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResetAllInstruments.Image = ((System.Drawing.Image)(resources.GetObject("btnResetAllInstruments.Image")));
            this.btnResetAllInstruments.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnResetAllInstruments.Name = "btnResetAllInstruments";
            this.btnResetAllInstruments.Size = new System.Drawing.Size(37, 20);
            this.btnResetAllInstruments.Text = "Clear";
            this.btnResetAllInstruments.ToolTipText = "CLEAR ALL INSTRUMENTS";
            // 
            // toolStripSeparator19
            // 
            this.toolStripSeparator19.Name = "toolStripSeparator19";
            this.toolStripSeparator19.Size = new System.Drawing.Size(6, 23);
            // 
            // btnCQGRecon
            // 
            this.btnCQGRecon.BackColor = System.Drawing.SystemColors.Control;
            this.btnCQGRecon.ForeColor = System.Drawing.Color.Black;
            this.btnCQGRecon.Image = ((System.Drawing.Image)(resources.GetObject("btnCQGRecon.Image")));
            this.btnCQGRecon.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCQGRecon.Name = "btnCQGRecon";
            this.btnCQGRecon.Size = new System.Drawing.Size(55, 20);
            this.btnCQGRecon.Text = "Reset";
            this.btnCQGRecon.ToolTipText = "CQG Reset";
            this.btnCQGRecon.Click += new System.EventHandler(this.btnCQGRecon_Click);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(6, 23);
            // 
            // mainRealtimeMenuStrip
            // 
            this.mainRealtimeMenuStrip.BackColor = System.Drawing.SystemColors.Control;
            this.mainRealtimeMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnCallAllInstruments,
            this.toolStripSeparator17,
            this.btnCallUnsubscribed,
            this.toolStripSeparator18,
            this.btnResetAllInstruments,
            this.toolStripSeparator19,
            this.btnCQGRecon,
            this.toolStripButton3});
            this.mainRealtimeMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mainRealtimeMenuStrip.Name = "mainRealtimeMenuStrip";
            this.mainRealtimeMenuStrip.Size = new System.Drawing.Size(795, 27);
            this.mainRealtimeMenuStrip.TabIndex = 8;
            this.mainRealtimeMenuStrip.Text = "menuStrip1";
            // 
            // richTextBoxLog
            // 
            this.richTextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxLog.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.Size = new System.Drawing.Size(498, 364);
            this.richTextBoxLog.TabIndex = 0;
            this.richTextBoxLog.Text = "";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.expressionListDataGrid);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxLog);
            this.splitContainer1.Size = new System.Drawing.Size(795, 364);
            this.splitContainer1.SplitterDistance = 293;
            this.splitContainer1.TabIndex = 10;
            // 
            // expressionListDataGrid
            // 
            this.expressionListDataGrid.AllowUserToAddRows = false;
            this.expressionListDataGrid.AllowUserToDeleteRows = false;
            this.expressionListDataGrid.AllowUserToResizeRows = false;
            this.expressionListDataGrid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.expressionListDataGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.expressionListDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.expressionListDataGrid.ColumnHeadersVisible = false;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.expressionListDataGrid.DefaultCellStyle = dataGridViewCellStyle2;
            this.expressionListDataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.expressionListDataGrid.Location = new System.Drawing.Point(0, 0);
            this.expressionListDataGrid.Name = "expressionListDataGrid";
            this.expressionListDataGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.expressionListDataGrid.Size = new System.Drawing.Size(293, 364);
            this.expressionListDataGrid.TabIndex = 7;
            // 
            // RealtimeDataManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(795, 413);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.mainRealtimeMenuStrip);
            this.Controls.Add(this.statusStripOptionMonitor);
            this.Name = "RealtimeDataManagement";
            this.Text = "RealtimeDataManagement";
            this.Load += new System.EventHandler(this.RealtimeDataManagement_Load);
            this.statusStripOptionMonitor.ResumeLayout(false);
            this.statusStripOptionMonitor.PerformLayout();
            this.mainRealtimeMenuStrip.ResumeLayout(false);
            this.mainRealtimeMenuStrip.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.expressionListDataGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStripOptionMonitor;
        private System.Windows.Forms.ToolStripStatusLabel ConnectionStatus;
        private System.Windows.Forms.ToolStripStatusLabel StatusSubscribeData;
        private System.Windows.Forms.ToolStripStatusLabel DataStatus;
        private System.Windows.Forms.ToolStripButton btnCallAllInstruments;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
        private System.Windows.Forms.ToolStripButton btnCallUnsubscribed;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
        private System.Windows.Forms.ToolStripButton btnResetAllInstruments;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
        private System.Windows.Forms.ToolStripButton btnCQGRecon;
        private System.Windows.Forms.ToolStripSeparator toolStripButton3;
        private System.Windows.Forms.MenuStrip mainRealtimeMenuStrip;
        private System.Windows.Forms.RichTextBox richTextBoxLog;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView expressionListDataGrid;
    }
}

