using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UTFEditor
{    

    public partial class RecalculateVertexDataForm : Form
    {
        public double MaxAngle { get; private set; }
        public bool RecalculateNormals { get; private set; }

        public RecalculateVertexDataForm()
        {
            InitializeComponent();
            MaxAngle = 30;
            textBoxMaxAngle.Text = MaxAngle.ToString();
            RecalculateNormals = false;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void textBoxMaxAngle_Validating(object sender, CancelEventArgs e)
        {
            double degrees = 0;
            if (double.TryParse(textBoxMaxAngle.Text, NumberStyles.Float,CultureInfo.InvariantCulture.NumberFormat, out degrees) )          
                e.Cancel = false;
            else    
                e.Cancel=true;
            MaxAngle = degrees;
        }

        private void checkBoxRecalculateNormals_CheckedChanged(object sender, EventArgs e)
        {
            RecalculateNormals = checkBoxRecalculateNormals.Checked;
            labelSmoothingAngle.Enabled = RecalculateNormals;
            textBoxMaxAngle.Enabled = RecalculateNormals;
        }
    }
}
