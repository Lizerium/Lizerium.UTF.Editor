namespace UTFEditor
{
    partial class RecalculateVertexDataForm
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
            this.checkBoxRecalculateNormals = new System.Windows.Forms.CheckBox();
            this.labelSmoothingAngle = new System.Windows.Forms.Label();
            this.textBoxMaxAngle = new System.Windows.Forms.TextBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // checkBoxRecalculateNormals
            // 
            this.checkBoxRecalculateNormals.AutoSize = true;
            this.checkBoxRecalculateNormals.Location = new System.Drawing.Point(12, 12);
            this.checkBoxRecalculateNormals.Name = "checkBoxRecalculateNormals";
            this.checkBoxRecalculateNormals.Size = new System.Drawing.Size(122, 17);
            this.checkBoxRecalculateNormals.TabIndex = 0;
            this.checkBoxRecalculateNormals.Text = "Recalculate normals";
            this.checkBoxRecalculateNormals.UseVisualStyleBackColor = true;
            this.checkBoxRecalculateNormals.CheckedChanged += new System.EventHandler(this.checkBoxRecalculateNormals_CheckedChanged);
            // 
            // labelSmoothingAngle
            // 
            this.labelSmoothingAngle.AutoSize = true;
            this.labelSmoothingAngle.Enabled = false;
            this.labelSmoothingAngle.Location = new System.Drawing.Point(9, 32);
            this.labelSmoothingAngle.Name = "labelSmoothingAngle";
            this.labelSmoothingAngle.Size = new System.Drawing.Size(230, 13);
            this.labelSmoothingAngle.TabIndex = 1;
            this.labelSmoothingAngle.Text = "Maximum angle for normal smoothing (degrees):";
            // 
            // textBoxMaxAngle
            // 
            this.textBoxMaxAngle.Enabled = false;
            this.textBoxMaxAngle.Location = new System.Drawing.Point(240, 29);
            this.textBoxMaxAngle.MaxLength = 10;
            this.textBoxMaxAngle.Name = "textBoxMaxAngle";
            this.textBoxMaxAngle.Size = new System.Drawing.Size(48, 20);
            this.textBoxMaxAngle.TabIndex = 2;
            this.textBoxMaxAngle.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxMaxAngle_Validating);
            // 
            // buttonOk
            // 
            this.buttonOk.Location = new System.Drawing.Point(81, 66);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 3;
            this.buttonOk.Text = "Ok";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(179, 66);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // RecalculateVertexDataForm
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(343, 101);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.textBoxMaxAngle);
            this.Controls.Add(this.labelSmoothingAngle);
            this.Controls.Add(this.checkBoxRecalculateNormals);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "RecalculateVertexDataForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Recalculate Vertex Data";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxRecalculateNormals;
        private System.Windows.Forms.Label labelSmoothingAngle;
        private System.Windows.Forms.TextBox textBoxMaxAngle;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
    }
}