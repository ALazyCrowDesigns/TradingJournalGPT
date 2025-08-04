namespace TradingJournalGPT.Forms
{
    partial class AnalysisResultForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblMessage = new System.Windows.Forms.Label();
            this.lblSymbolLabel = new System.Windows.Forms.Label();
            this.lblSymbol = new System.Windows.Forms.Label();
            this.lblDateLabel = new System.Windows.Forms.Label();
            this.lblDate = new System.Windows.Forms.Label();
            this.lblPreviousCloseLabel = new System.Windows.Forms.Label();
            this.lblPreviousClose = new System.Windows.Forms.Label();
            this.lblHighAfterVolumeSurgeLabel = new System.Windows.Forms.Label();
            this.lblHighAfterVolumeSurge = new System.Windows.Forms.Label();
            this.lblLowAfterVolumeSurgeLabel = new System.Windows.Forms.Label();
            this.lblLowAfterVolumeSurge = new System.Windows.Forms.Label();
            this.lblGapPercentToHighLabel = new System.Windows.Forms.Label();
            this.lblGapPercentToHigh = new System.Windows.Forms.Label();
            this.lblGapPercentHighToLowLabel = new System.Windows.Forms.Label();
            this.lblGapPercentHighToLow = new System.Windows.Forms.Label();
            this.lblVolumeLabel = new System.Windows.Forms.Label();
            this.lblVolume = new System.Windows.Forms.Label();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSaveLocally = new System.Windows.Forms.Button();
            this.btnSaveToGoogleSheets = new System.Windows.Forms.Button();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.Location = new System.Drawing.Point(12, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(147, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Analysis Results";
            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = true;
            this.lblMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblMessage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblMessage.Location = new System.Drawing.Point(12, 34);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(360, 15);
            this.lblMessage.TabIndex = 1;
            this.lblMessage.Text = "Do you want to save the data locally or upload it to Google Sheets?";
            // 
            // lblSymbolLabel
            // 
            this.lblSymbolLabel.AutoSize = true;
            this.lblSymbolLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblSymbolLabel.Location = new System.Drawing.Point(12, 60);
            this.lblSymbolLabel.Name = "lblSymbolLabel";
            this.lblSymbolLabel.Size = new System.Drawing.Size(47, 15);
            this.lblSymbolLabel.TabIndex = 1;
            this.lblSymbolLabel.Text = "Symbol:";
            // 
            // lblSymbol
            // 
            this.lblSymbol.AutoSize = true;
            this.lblSymbol.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblSymbol.Location = new System.Drawing.Point(120, 60);
            this.lblSymbol.Name = "lblSymbol";
            this.lblSymbol.Size = new System.Drawing.Size(41, 15);
            this.lblSymbol.TabIndex = 2;
            this.lblSymbol.Text = "SYMBOL";
            // 
            // lblDateLabel
            // 
            this.lblDateLabel.AutoSize = true;
            this.lblDateLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblDateLabel.Location = new System.Drawing.Point(12, 85);
            this.lblDateLabel.Name = "lblDateLabel";
            this.lblDateLabel.Size = new System.Drawing.Size(37, 15);
            this.lblDateLabel.TabIndex = 3;
            this.lblDateLabel.Text = "Date:";
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblDate.Location = new System.Drawing.Point(120, 85);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(34, 15);
            this.lblDate.TabIndex = 4;
            this.lblDate.Text = "DATE";
            // 
            // lblPreviousCloseLabel
            // 
            this.lblPreviousCloseLabel.AutoSize = true;
            this.lblPreviousCloseLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblPreviousCloseLabel.Location = new System.Drawing.Point(12, 110);
            this.lblPreviousCloseLabel.Name = "lblPreviousCloseLabel";
            this.lblPreviousCloseLabel.Size = new System.Drawing.Size(95, 15);
            this.lblPreviousCloseLabel.TabIndex = 5;
            this.lblPreviousCloseLabel.Text = "Previous Close:";
            // 
            // lblPreviousClose
            // 
            this.lblPreviousClose.AutoSize = true;
            this.lblPreviousClose.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblPreviousClose.Location = new System.Drawing.Point(120, 110);
            this.lblPreviousClose.Name = "lblPreviousClose";
            this.lblPreviousClose.Size = new System.Drawing.Size(34, 15);
            this.lblPreviousClose.TabIndex = 6;
            this.lblPreviousClose.Text = "$0.00";
            // 
            // lblHighAfterVolumeSurgeLabel
            // 
            this.lblHighAfterVolumeSurgeLabel.AutoSize = true;
            this.lblHighAfterVolumeSurgeLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblHighAfterVolumeSurgeLabel.Location = new System.Drawing.Point(12, 135);
            this.lblHighAfterVolumeSurgeLabel.Name = "lblHighAfterVolumeSurgeLabel";
            this.lblHighAfterVolumeSurgeLabel.Size = new System.Drawing.Size(147, 15);
            this.lblHighAfterVolumeSurgeLabel.TabIndex = 7;
            this.lblHighAfterVolumeSurgeLabel.Text = "High After Volume Surge:";
            // 
            // lblHighAfterVolumeSurge
            // 
            this.lblHighAfterVolumeSurge.AutoSize = true;
            this.lblHighAfterVolumeSurge.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblHighAfterVolumeSurge.Location = new System.Drawing.Point(165, 135);
            this.lblHighAfterVolumeSurge.Name = "lblHighAfterVolumeSurge";
            this.lblHighAfterVolumeSurge.Size = new System.Drawing.Size(34, 15);
            this.lblHighAfterVolumeSurge.TabIndex = 8;
            this.lblHighAfterVolumeSurge.Text = "$0.00";
            // 
            // lblLowAfterVolumeSurgeLabel
            // 
            this.lblLowAfterVolumeSurgeLabel.AutoSize = true;
            this.lblLowAfterVolumeSurgeLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLowAfterVolumeSurgeLabel.Location = new System.Drawing.Point(12, 160);
            this.lblLowAfterVolumeSurgeLabel.Name = "lblLowAfterVolumeSurgeLabel";
            this.lblLowAfterVolumeSurgeLabel.Size = new System.Drawing.Size(145, 15);
            this.lblLowAfterVolumeSurgeLabel.TabIndex = 9;
            this.lblLowAfterVolumeSurgeLabel.Text = "Low After Volume Surge:";
            // 
            // lblLowAfterVolumeSurge
            // 
            this.lblLowAfterVolumeSurge.AutoSize = true;
            this.lblLowAfterVolumeSurge.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblLowAfterVolumeSurge.Location = new System.Drawing.Point(165, 160);
            this.lblLowAfterVolumeSurge.Name = "lblLowAfterVolumeSurge";
            this.lblLowAfterVolumeSurge.Size = new System.Drawing.Size(34, 15);
            this.lblLowAfterVolumeSurge.TabIndex = 10;
            this.lblLowAfterVolumeSurge.Text = "$0.00";
            // 
            // lblGapPercentToHighLabel
            // 
            this.lblGapPercentToHighLabel.AutoSize = true;
            this.lblGapPercentToHighLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblGapPercentToHighLabel.Location = new System.Drawing.Point(12, 185);
            this.lblGapPercentToHighLabel.Name = "lblGapPercentToHighLabel";
            this.lblGapPercentToHighLabel.Size = new System.Drawing.Size(127, 15);
            this.lblGapPercentToHighLabel.TabIndex = 11;
            this.lblGapPercentToHighLabel.Text = "Gap % (Close to High):";
            // 
            // lblGapPercentToHigh
            // 
            this.lblGapPercentToHigh.AutoSize = true;
            this.lblGapPercentToHigh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblGapPercentToHigh.Location = new System.Drawing.Point(145, 185);
            this.lblGapPercentToHigh.Name = "lblGapPercentToHigh";
            this.lblGapPercentToHigh.Size = new System.Drawing.Size(25, 15);
            this.lblGapPercentToHigh.TabIndex = 12;
            this.lblGapPercentToHigh.Text = "0%";
            // 
            // lblGapPercentHighToLowLabel
            // 
            this.lblGapPercentHighToLowLabel.AutoSize = true;
            this.lblGapPercentHighToLowLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblGapPercentHighToLowLabel.Location = new System.Drawing.Point(12, 210);
            this.lblGapPercentHighToLowLabel.Name = "lblGapPercentHighToLowLabel";
            this.lblGapPercentHighToLowLabel.Size = new System.Drawing.Size(125, 15);
            this.lblGapPercentHighToLowLabel.TabIndex = 13;
            this.lblGapPercentHighToLowLabel.Text = "Gap % (High to Low):";
            // 
            // lblGapPercentHighToLow
            // 
            this.lblGapPercentHighToLow.AutoSize = true;
            this.lblGapPercentHighToLow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblGapPercentHighToLow.Location = new System.Drawing.Point(145, 210);
            this.lblGapPercentHighToLow.Name = "lblGapPercentHighToLow";
            this.lblGapPercentHighToLow.Size = new System.Drawing.Size(25, 15);
            this.lblGapPercentHighToLow.TabIndex = 14;
            this.lblGapPercentHighToLow.Text = "0%";
            // 
            // lblVolumeLabel
            // 
            this.lblVolumeLabel.AutoSize = true;
            this.lblVolumeLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblVolumeLabel.Location = new System.Drawing.Point(12, 235);
            this.lblVolumeLabel.Name = "lblVolumeLabel";
            this.lblVolumeLabel.Size = new System.Drawing.Size(54, 15);
            this.lblVolumeLabel.TabIndex = 15;
            this.lblVolumeLabel.Text = "Volume:";
            // 
            // lblVolume
            // 
            this.lblVolume.AutoSize = true;
            this.lblVolume.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblVolume.Location = new System.Drawing.Point(120, 235);
            this.lblVolume.Name = "lblVolume";
            this.lblVolume.Size = new System.Drawing.Size(25, 15);
            this.lblVolume.TabIndex = 16;
            this.lblVolume.Text = "0M";
            // 
            // panelButtons
            // 
            this.panelButtons.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.panelButtons.Controls.Add(this.btnCancel);
            this.panelButtons.Controls.Add(this.btnSaveLocally);
            this.panelButtons.Controls.Add(this.btnSaveToGoogleSheets);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 270);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(384, 60);
            this.panelButtons.TabIndex = 17;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(297, 15);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSaveLocally
            // 
            this.btnSaveLocally.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveLocally.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
            this.btnSaveLocally.FlatAppearance.BorderSize = 0;
            this.btnSaveLocally.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveLocally.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnSaveLocally.ForeColor = System.Drawing.Color.White;
            this.btnSaveLocally.Location = new System.Drawing.Point(216, 15);
            this.btnSaveLocally.Name = "btnSaveLocally";
            this.btnSaveLocally.Size = new System.Drawing.Size(75, 30);
            this.btnSaveLocally.TabIndex = 1;
            this.btnSaveLocally.Text = "Upload";
            this.btnSaveLocally.UseVisualStyleBackColor = false;
            this.btnSaveLocally.Click += new System.EventHandler(this.btnSaveLocally_Click);
            // 
            // btnSaveToGoogleSheets
            // 
            this.btnSaveToGoogleSheets.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveToGoogleSheets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSaveToGoogleSheets.FlatAppearance.BorderSize = 0;
            this.btnSaveToGoogleSheets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveToGoogleSheets.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnSaveToGoogleSheets.ForeColor = System.Drawing.Color.White;
            this.btnSaveToGoogleSheets.Location = new System.Drawing.Point(135, 15);
            this.btnSaveToGoogleSheets.Name = "btnSaveToGoogleSheets";
            this.btnSaveToGoogleSheets.Size = new System.Drawing.Size(75, 30);
            this.btnSaveToGoogleSheets.TabIndex = 0;
            this.btnSaveToGoogleSheets.Text = "Local";
            this.btnSaveToGoogleSheets.UseVisualStyleBackColor = false;
            this.btnSaveToGoogleSheets.Click += new System.EventHandler(this.btnSaveToGoogleSheets_Click);
            // 
            // AnalysisResultForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 330);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.lblVolume);
            this.Controls.Add(this.lblVolumeLabel);
            this.Controls.Add(this.lblGapPercentHighToLow);
            this.Controls.Add(this.lblGapPercentHighToLowLabel);
            this.Controls.Add(this.lblGapPercentToHigh);
            this.Controls.Add(this.lblGapPercentToHighLabel);
            this.Controls.Add(this.lblLowAfterVolumeSurge);
            this.Controls.Add(this.lblLowAfterVolumeSurgeLabel);
            this.Controls.Add(this.lblHighAfterVolumeSurge);
            this.Controls.Add(this.lblHighAfterVolumeSurgeLabel);
            this.Controls.Add(this.lblPreviousClose);
            this.Controls.Add(this.lblPreviousCloseLabel);
            this.Controls.Add(this.lblDate);
            this.Controls.Add(this.lblDateLabel);
            this.Controls.Add(this.lblSymbol);
            this.Controls.Add(this.lblSymbolLabel);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AnalysisResultForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Analysis Results";
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Label lblSymbolLabel;
        private System.Windows.Forms.Label lblSymbol;
        private System.Windows.Forms.Label lblDateLabel;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Label lblPreviousCloseLabel;
        private System.Windows.Forms.Label lblPreviousClose;
        private System.Windows.Forms.Label lblHighAfterVolumeSurgeLabel;
        private System.Windows.Forms.Label lblHighAfterVolumeSurge;
        private System.Windows.Forms.Label lblLowAfterVolumeSurgeLabel;
        private System.Windows.Forms.Label lblLowAfterVolumeSurge;
        private System.Windows.Forms.Label lblGapPercentToHighLabel;
        private System.Windows.Forms.Label lblGapPercentToHigh;
        private System.Windows.Forms.Label lblGapPercentHighToLowLabel;
        private System.Windows.Forms.Label lblGapPercentHighToLow;
        private System.Windows.Forms.Label lblVolumeLabel;
        private System.Windows.Forms.Label lblVolume;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSaveLocally;
        private System.Windows.Forms.Button btnSaveToGoogleSheets;
    }
} 