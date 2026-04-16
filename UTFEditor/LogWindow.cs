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
    public partial class LogWindow : Form
    {
        public LogWindow()
        {
            InitializeComponent();
        }
        public void SetWindowTitle(string title)
        {
            this.Text = title;
        }

        public void SetLogDescrition(string description)
        {
            this.labelLogDescription.Text = description;
        }

        public void AppendNormalLogText(string logEntry)
        {            
            this.richTextBoxLog.AppendText(logEntry + "\n");            
        }

        public void AppendBoldLogText(string logEntry)
        {
            this.richTextBoxLog.SelectionFont = new Font(this.richTextBoxLog.SelectionFont, FontStyle.Bold);
            this.richTextBoxLog.AppendText(logEntry + "\n");
            this.richTextBoxLog.SelectionFont = new Font(this.richTextBoxLog.SelectionFont, FontStyle.Regular);
        }

        public void ClearLog()
        {
            this.richTextBoxLog.Clear();
        }

    }
}
