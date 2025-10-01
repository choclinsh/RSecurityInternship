namespace AnomaliesDetector
{
    partial class MainForm
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

        
        private void InitializeComponent()
        {
            anomalyGrid = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)anomalyGrid).BeginInit();
            SuspendLayout();
            // 
            // anomalyGrid features
            // 
            anomalyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            anomalyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            anomalyGrid.Dock = DockStyle.Top;
            anomalyGrid.Location = new Point(0, 0);
            anomalyGrid.Name = "anomalyGrid";
            anomalyGrid.ReadOnly = true;
            anomalyGrid.Size = new Size(800, 400);
            anomalyGrid.TabIndex = 1;
            // 
            // Form1 features
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1500, 650);
            Controls.Add(anomalyGrid);
            Name = "Form1";
            Text = "Results: searching anomalies...";
            ((System.ComponentModel.ISupportInitialize)anomalyGrid).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView anomalyGrid;
    }
}
