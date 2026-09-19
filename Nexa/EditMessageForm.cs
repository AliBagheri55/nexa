using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nexa
{
    public partial class EditMessageForm : Form
    {
        private TextBox txtMessage;
        private Button btnSave;
        private Button btnCancel;

        public string EditedMessage
        {
            get
            {
                return txtMessage.Text.Trim();
            }
        }

        public EditMessageForm(string currentMessage)
        {
            InitializeComponent();

            Text = "ویرایش پیام";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(500, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            txtMessage = new TextBox();

            txtMessage.Multiline = true;
            txtMessage.ScrollBars = ScrollBars.Vertical;
            txtMessage.Font = new Font(
                "Segoe UI",
                11);

            txtMessage.Text = currentMessage;

            txtMessage.SetBounds(
                20,
                20,
                440,
                150);

            btnSave = new Button();

            btnSave.Text = "ذخیره";
            btnSave.SetBounds(
                260,
                190,
                100,
                40);

            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();

            btnCancel.Text = "لغو";
            btnCancel.SetBounds(
                150,
                190,
                100,
                40);

            btnCancel.DialogResult =
                DialogResult.Cancel;

            Controls.Add(txtMessage);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
        private void EditMessageForm_Load(object sender, EventArgs e)
        {

        }
        private void BtnSave_Click(
            object sender,
            EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                txtMessage.Text))
            {
                MessageBox.Show(
                    "متن پیام نمی‌تواند خالی باشد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            DialogResult =
                DialogResult.OK;

            Close();
        }
    }
}