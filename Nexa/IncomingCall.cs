using System;
using System.Windows.Forms;

namespace Nexa
{
    public partial class IncomingCall : Form
    {
        private int callerId;
        private int receiverId;
        private string callerName;

        public IncomingCall(
            int callerId,
            int receiverId,
            string callerName)
        {
            InitializeComponent();

            this.callerId = callerId;
            this.receiverId = receiverId;
            this.callerName = callerName;

            lblCaller.Text =
                callerName + " در حال تماس تصویری است...";
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnReject_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAccept_Click_1(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}