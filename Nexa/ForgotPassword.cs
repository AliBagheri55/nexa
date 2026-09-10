using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class ForgotPassword : Form
    {
        [DllImport(@"C:\Users\Pixel\Desktop\NexaSecurity1\x64\Debug\NexaSecurity1.dll",EntryPoint = "CheckSecurityCode",CallingConvention = CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
        private static extern bool CheckSecurityCode(string code);
        [DllImport(@"C:\Users\Pixel\Desktop\NexaSecurity1\x64\Debug\NexaSecurity1.dll",EntryPoint = "UpdateUserBySecurityCode",CallingConvention = CallingConvention.Cdecl,CharSet = CharSet.Ansi)]
        private static extern bool UpdateUserBySecurityCode(string code,string newUsername,string newPassword);

        public ForgotPassword()
        {
            InitializeComponent();
        }

        private void lbllBack_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();
            frmLogin login = new frmLogin();
            login.ShowDialog();
        }

        private void ForgotPassword_Load(object sender, EventArgs e)
        {
            txtNewUsername.Enabled = false;
            txtNewPassword.Enabled = false;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
        
        }

        private void btnSecurityCode_Click(object sender, EventArgs e)
        {
            string code = txtSecurityCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("لطفاً کد امنیتی را وارد کنید.");
                return;
            }

            bool result = CheckSecurityCode(code);

            if (result)
            {
                txtNewUsername.Enabled = true;
                txtNewPassword.Enabled = true;

                MessageBox.Show("کد امنیتی صحیح است.");
            }
            else
            {
                txtNewUsername.Enabled = false;
                txtNewPassword.Enabled = false;

                MessageBox.Show("کد امنیتی اشتباه است.");
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!txtNewUsername.Enabled || !txtNewPassword.Enabled)
            {
                MessageBox.Show("ابتدا کد امنیتی را تأیید کنید.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNewUsername.Text) ||
                string.IsNullOrWhiteSpace(txtNewPassword.Text))
            {
                MessageBox.Show("نام کاربری و رمز عبور جدید را وارد کنید.");
                return;
            }

            string code = txtSecurityCode.Text.Trim();

            bool result = UpdateUserBySecurityCode(
                code,
                txtNewUsername.Text.Trim(),
                txtNewPassword.Text
            );

            if (result)
            {
                MessageBox.Show("اطلاعات با موفقیت تغییر کرد.");

                txtSecurityCode.Clear();
                txtNewUsername.Clear();
                txtNewPassword.Clear();

                txtNewUsername.Enabled = false;
                txtNewPassword.Enabled = false;
            }
            else
            {
                MessageBox.Show("کد امنیتی پیدا نشد یا عملیات ناموفق بود.");
            }
        }
    }
}
