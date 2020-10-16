﻿/*  GRBL-Plotter. Another GCode sender for GRBL.
    This file is part of the GRBL-Plotter application.
   
    Copyright (C) 2015-2020 Sven Hasemann contact: svenhb@web.de

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
namespace GRBL_Plotter
{
    partial class GCodeFromText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GCodeFromText));
            this.label1 = new System.Windows.Forms.Label();
            this.cBFont = new System.Windows.Forms.ComboBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cBConnectLetter = new System.Windows.Forms.CheckBox();
            this.cBTool = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cBPauseLine = new System.Windows.Forms.CheckBox();
            this.cBPauseWord = new System.Windows.Forms.CheckBox();
            this.cBPauseChar = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.cBToolTable = new System.Windows.Forms.CheckBox();
            this.nUDFontLine = new System.Windows.Forms.NumericUpDown();
            this.nUDFontDistance = new System.Windows.Forms.NumericUpDown();
            this.nUDFontSize = new System.Windows.Forms.NumericUpDown();
            this.tBText = new System.Windows.Forms.TextBox();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nUDFontLine)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUDFontDistance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUDFontSize)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // cBFont
            // 
            this.cBFont.FormattingEnabled = true;
            resources.ApplyResources(this.cBFont, "cBFont");
            this.cBFont.Name = "cBFont";
            // 
            // btnApply
            // 
            resources.ApplyResources(this.btnApply, "btnApply");
            this.btnApply.Name = "btnApply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.nUDFontLine);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.nUDFontDistance);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.nUDFontSize);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.cBFont);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            this.toolTip1.SetToolTip(this.label11, resources.GetString("label11.ToolTip"));
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            this.toolTip1.SetToolTip(this.label10, resources.GetString("label10.ToolTip"));
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cBToolTable);
            this.groupBox1.Controls.Add(this.cBConnectLetter);
            this.groupBox1.Controls.Add(this.cBTool);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.cBPauseLine);
            this.groupBox1.Controls.Add(this.cBPauseWord);
            this.groupBox1.Controls.Add(this.cBPauseChar);
            this.groupBox1.Controls.Add(this.label2);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // cBConnectLetter
            // 
            resources.ApplyResources(this.cBConnectLetter, "cBConnectLetter");
            this.cBConnectLetter.Name = "cBConnectLetter";
            this.toolTip1.SetToolTip(this.cBConnectLetter, resources.GetString("cBConnectLetter.ToolTip"));
            this.cBConnectLetter.UseVisualStyleBackColor = true;
            // 
            // cBTool
            // 
            this.cBTool.FormattingEnabled = true;
            resources.ApplyResources(this.cBTool, "cBTool");
            this.cBTool.Name = "cBTool";
            this.cBTool.SelectedIndexChanged += new System.EventHandler(this.cBTool_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // cBPauseLine
            // 
            resources.ApplyResources(this.cBPauseLine, "cBPauseLine");
            this.cBPauseLine.Name = "cBPauseLine";
            this.cBPauseLine.UseVisualStyleBackColor = true;
            // 
            // cBPauseWord
            // 
            resources.ApplyResources(this.cBPauseWord, "cBPauseWord");
            this.cBPauseWord.Name = "cBPauseWord";
            this.cBPauseWord.UseVisualStyleBackColor = true;
            // 
            // cBPauseChar
            // 
            resources.ApplyResources(this.cBPauseChar, "cBPauseChar");
            this.cBPauseChar.Name = "cBPauseChar";
            this.cBPauseChar.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // cBToolTable
            // 
            resources.ApplyResources(this.cBToolTable, "cBToolTable");
            this.cBToolTable.Checked = global::GRBL_Plotter.Properties.Settings.Default.importGCToolTableUse;
            this.cBToolTable.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::GRBL_Plotter.Properties.Settings.Default, "importGCToolTableUse", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cBToolTable.Name = "cBToolTable";
            this.toolTip1.SetToolTip(this.cBToolTable, resources.GetString("cBToolTable.ToolTip"));
            this.cBToolTable.UseVisualStyleBackColor = true;
            this.cBToolTable.CheckedChanged += new System.EventHandler(this.cBToolTable_CheckedChanged);
            // 
            // nUDFontLine
            // 
            this.nUDFontLine.DecimalPlaces = 1;
            resources.ApplyResources(this.nUDFontLine, "nUDFontLine");
            this.nUDFontLine.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nUDFontLine.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nUDFontLine.Name = "nUDFontLine";
            this.nUDFontLine.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // nUDFontDistance
            // 
            this.nUDFontDistance.DecimalPlaces = 1;
            this.nUDFontDistance.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            resources.ApplyResources(this.nUDFontDistance, "nUDFontDistance");
            this.nUDFontDistance.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.nUDFontDistance.Name = "nUDFontDistance";
            // 
            // nUDFontSize
            // 
            this.nUDFontSize.DecimalPlaces = 1;
            resources.ApplyResources(this.nUDFontSize, "nUDFontSize");
            this.nUDFontSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nUDFontSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nUDFontSize.Name = "nUDFontSize";
            this.nUDFontSize.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nUDFontSize.ValueChanged += new System.EventHandler(this.nUDFontSize_ValueChanged);
            // 
            // tBText
            // 
            this.tBText.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::GRBL_Plotter.Properties.Settings.Default, "createtextFontText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            resources.ApplyResources(this.tBText, "tBText");
            this.tBText.Name = "tBText";
            this.tBText.Text = global::GRBL_Plotter.Properties.Settings.Default.createTextFontText;
            // 
            // GCodeFromText
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.tBText);
            this.Name = "GCodeFromText";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TextForm_FormClosing);
            this.Load += new System.EventHandler(this.TextForm_Load);
            this.Resize += new System.EventHandler(this.GCodeFromText_Resize);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nUDFontLine)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUDFontDistance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUDFontSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cBConnectLetter;
        private System.Windows.Forms.CheckBox cBPauseChar;
        private System.Windows.Forms.CheckBox cBPauseLine;
        private System.Windows.Forms.CheckBox cBPauseWord;
        private System.Windows.Forms.CheckBox cBToolTable;
        private System.Windows.Forms.ComboBox cBFont;
        private System.Windows.Forms.ComboBox cBTool;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nUDFontDistance;
        private System.Windows.Forms.NumericUpDown nUDFontLine;
        private System.Windows.Forms.NumericUpDown nUDFontSize;
        private System.Windows.Forms.TextBox tBText;
        private System.Windows.Forms.ToolTip toolTip1;
        public System.Windows.Forms.Button btnApply;
    }
}