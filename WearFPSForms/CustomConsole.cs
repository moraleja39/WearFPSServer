using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WearFPSForms {
    public partial class CustomConsole : Form {
        public CustomConsole() {
            InitializeComponent();
            textBox.Enter += TextBox_Enter;
        }

        private void TextBox_Enter(object sender, EventArgs e) {
            ActiveControl = label1;
        }

        public void Write(string text) {
            textBox.BeginInvoke((Action)(() => textBox.AppendText(text + Environment.NewLine) ));
        }
    }
}
