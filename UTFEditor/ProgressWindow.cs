using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UTFEditor
{
    public partial class ProgressWindow : Form
    {
        private bool processWasAborted = false;
        public ProgressWindow()
        {
            InitializeComponent();
        }

        private void ProgressWindow_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }
        
        public void SetProgressText(string progressText)
        {
            labelProgressText.Text = progressText;
        }

        public void SetWindowTitle(string title)
        {
            this.Text = title;
        }

        public void AdvanceProgressStep()
        {
            this.progressBar1.Increment(1);
        }

        public void SetMaxmimumProgressSteps(int maximum)
        {
            this.progressBar1.Maximum = maximum;
        }

        public bool ProcessWasAborted()
        {
            return processWasAborted;
        }

        private void buttonAbort_Click(object sender, EventArgs e)
        {
            processWasAborted = true;
        }
    }
}
