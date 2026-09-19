using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Nexa
{
    public partial class ForgotPassword : Form
    {
        private static readonly HttpClient httpClient =
            new HttpClient();

        private const string ApiBaseUrl =
            "https://localhost:7199";

        public ForgotPassword()
        {
            InitializeComponent();
        }

        private void lbllBack_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            Hide();

            frmLogin login =
                new frmLogin();

            login.ShowDialog();
        }

        private void ForgotPassword_Load(
            object sender,
            EventArgs e)
        {
            txtNewUsername.Enabled =
                false;

            txtNewPassword.Enabled =
                false;
        }

        private async void btnSearch_Click(
            object sender,
            EventArgs e)
        {
            await CheckSecurityCode();
        }

        private async void btnSecurityCode_Click(
            object sender,
            EventArgs e)
        {
            await CheckSecurityCode();
        }

        private async System.Threading.Tasks.Task
            CheckSecurityCode()
        {
            string code =
                txtSecurityCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show(
                    "لطفاً کد امنیتی را وارد کنید.");

                return;
            }

            try
            {
                var requestData =
                    new
                    {
                        code = code
                    };

                string json =
                    JsonSerializer.Serialize(
                        requestData);

                StringContent content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                HttpResponseMessage response =
                    await httpClient.PostAsync(
                        ApiBaseUrl +
                        "/api/ForgotPassword/check-security-code",
                        content);

                content.Dispose();

                string responseText =
                    await response.Content
                        .ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    txtNewUsername.Enabled =
                        true;

                    txtNewPassword.Enabled =
                        true;

                    MessageBox.Show(
                        "کد امنیتی صحیح است.");

                    return;
                }

                txtNewUsername.Enabled =
                    false;

                txtNewPassword.Enabled =
                    false;

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show(
                        "کد امنیتی اشتباه است.");

                    return;
                }

                ShowApiError(
                    responseText,
                    "خطا در بررسی کد امنیتی.");
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    "ارتباط با Nexa.Api برقرار نشد.\n\n" +
                    "آدرس API:\n" +
                    ApiBaseUrl +
                    "\n\n" +
                    ex.Message);
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                MessageBox.Show(
                    "درخواست بیش از حد طول کشید.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بررسی کد:\n\n" +
                    ex.Message);
            }
        }

        private async void btnUpdate_Click(
            object sender,
            EventArgs e)
        {
            if (!txtNewUsername.Enabled ||
                !txtNewPassword.Enabled)
            {
                MessageBox.Show(
                    "ابتدا کد امنیتی را تأیید کنید.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtNewUsername.Text))
            {
                MessageBox.Show(
                    "نام کاربری جدید را وارد کنید.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtNewPassword.Text))
            {
                MessageBox.Show(
                    "رمز عبور جدید را وارد کنید.");

                return;
            }

            string code =
                txtSecurityCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show(
                    "کد امنیتی وارد نشده است.");

                return;
            }

            try
            {
                var requestData =
                    new
                    {
                        code = code,

                        newUsername =
                            txtNewUsername.Text.Trim(),

                        newPassword =
                            txtNewPassword.Text
                    };

                string json =
                    JsonSerializer.Serialize(
                        requestData);

                StringContent content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                HttpResponseMessage response =
                    await httpClient.PostAsync(
                        ApiBaseUrl +
                        "/api/ForgotPassword/update-by-security-code",
                        content);

                content.Dispose();

                string responseText =
                    await response.Content
                        .ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        "اطلاعات با موفقیت تغییر کرد.");

                    txtSecurityCode.Clear();

                    txtNewUsername.Clear();

                    txtNewPassword.Clear();

                    txtNewUsername.Enabled =
                        false;

                    txtNewPassword.Enabled =
                        false;

                    return;
                }

                if (response.StatusCode ==
                    HttpStatusCode.Conflict)
                {
                    MessageBox.Show(
                        "این نام کاربری قبلاً استفاده شده است.");

                    return;
                }

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show(
                        "کد امنیتی معتبر نیست.");

                    txtNewUsername.Enabled =
                        false;

                    txtNewPassword.Enabled =
                        false;

                    return;
                }

                ShowApiError(
                    responseText,
                    "خطا در تغییر اطلاعات.");
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    "ارتباط با Nexa.Api برقرار نشد.\n\n" +
                    "آدرس API:\n" +
                    ApiBaseUrl +
                    "\n\n" +
                    ex.Message);
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                MessageBox.Show(
                    "درخواست بیش از حد طول کشید.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در تغییر اطلاعات:\n\n" +
                    ex.Message);
            }
        }

        private void ShowApiError(
            string responseText,
            string defaultMessage)
        {
            try
            {
                JsonDocument document =
                    JsonDocument.Parse(
                        responseText);

                JsonElement messageElement;

                if (document.RootElement.TryGetProperty(
                    "message",
                    out messageElement))
                {
                    string message =
                        messageElement.GetString();

                    document.Dispose();

                    MessageBox.Show(
                        string.IsNullOrWhiteSpace(message)
                            ? defaultMessage
                            : message);

                    return;
                }

                document.Dispose();

                MessageBox.Show(
                    defaultMessage +
                    "\n\n" +
                    responseText);
            }
            catch
            {
                MessageBox.Show(
                    defaultMessage +
                    "\n\n" +
                    responseText);
            }
        }
    }
}