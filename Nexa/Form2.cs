using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class CreateAnAccount : Form
    {
        private string selectedPhotoPath = "";
        private string captchaCode;

        private static readonly HttpClient httpClient =
            new HttpClient();

        private const string ApiBaseUrl =
            "https://localhost:7199";

        public CreateAnAccount()
        {
            InitializeComponent();
        }

        private void CreateAnAccount_Load(
            object sender,
            EventArgs e)
        {
            GenerateCaptcha();
        }

        private void GenerateCaptcha()
        {
            const string chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            Random random =
                new Random();

            captchaCode = "";

            for (int i = 0; i < 5; i++)
            {
                captchaCode +=
                    chars[random.Next(chars.Length)];
            }

            Bitmap bitmap =
                new Bitmap(
                    picSecurityCode.Width,
                    picSecurityCode.Height);

            using (Graphics g =
                   Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                using (Font font =
                       new Font(
                           "Arial",
                           24,
                           FontStyle.Bold))
                {
                    g.DrawString(
                        captchaCode,
                        font,
                        Brushes.Black,
                        20,
                        10);
                }
            }

            if (picSecurityCode.Image != null)
            {
                picSecurityCode.Image.Dispose();
            }

            picSecurityCode.Image =
                bitmap;
        }

        private void btnProfilePhoto_Click(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog openFileDialog =
                   new OpenFileDialog())
            {
                openFileDialog.Title =
                    "انتخاب عکس";

                openFileDialog.Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

                openFileDialog.Multiselect =
                    false;

                if (openFileDialog.ShowDialog() ==
                    DialogResult.OK)
                {
                    selectedPhotoPath =
                        openFileDialog.FileName;

                    if (picPhoto.Image != null)
                    {
                        picPhoto.Image.Dispose();
                        picPhoto.Image = null;
                    }

                    using (Image tempImage =
                           Image.FromFile(
                               selectedPhotoPath))
                    {
                        picPhoto.Image =
                            new Bitmap(tempImage);
                    }

                    picPhoto.SizeMode =
                        PictureBoxSizeMode.Zoom;
                }
            }
        }

        private async void btnCreate_Click(
            object sender,
            EventArgs e)
        {
            if (txtSecurityCode.Text.Trim() !=
                captchaCode)
            {
                MessageBox.Show(
                    "کد کپچا اشتباه است!",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtSecurityCode.Clear();

                GenerateCaptcha();

                return;
            }

            if (txtPassword.Text !=
                txtRepeatPassword.Text)
            {
                MessageBox.Show(
                    "مقادیر وارد شده یکسان نیستند!",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtFirstAndLastName.Text))
            {
                MessageBox.Show(
                    "نام و نام خانوادگی را وارد کنید!",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtId.Text))
            {
                MessageBox.Show(
                    "YourID را وارد کنید!",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtPhoneNumber.Text))
            {
                MessageBox.Show(
                    "شماره تلفن را وارد کنید!",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtPassword.Text))
            {
                MessageBox.Show(
                    "رمز عبور را وارد کنید!",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                var registerRequest =
                    new
                    {
                        firstAndLastName =
                            txtFirstAndLastName.Text.Trim(),

                        yourID =
                            txtId.Text.Trim(),

                        phoneNumber =
                            txtPhoneNumber.Text.Trim(),

                        password =
                            txtPassword.Text,

                        bio =
                            string.IsNullOrWhiteSpace(
                                txtBio.Text)
                            ? null
                            : txtBio.Text.Trim(),

                        profilePhoto =
                            string.IsNullOrWhiteSpace(
                                selectedPhotoPath)
                            ? null
                            : selectedPhotoPath
                    };

                string json =
                    JsonSerializer.Serialize(
                        registerRequest);

                StringContent content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                string url =
                    ApiBaseUrl +
                    "/api/Auth/register";

                btnCreate.Enabled =
                    false;

                Cursor =
                    Cursors.WaitCursor;

                HttpResponseMessage response =
                    await httpClient.PostAsync(
                        url,
                        content);

                content.Dispose();

                string responseText =
                    await response.Content
                        .ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        "حساب با موفقیت ساخته شد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    txtFirstAndLastName.Clear();
                    txtId.Clear();
                    txtPhoneNumber.Clear();
                    txtPassword.Clear();
                    txtRepeatPassword.Clear();
                    txtBio.Clear();
                    txtSecurityCode.Clear();

                    Hide();

                    frmLogin login =
                        new frmLogin();

                    login.ShowDialog();

                    return;
                }

                if (response.StatusCode ==
                    HttpStatusCode.Conflict)
                {
                    MessageBox.Show(
                        "این YourID قبلاً ثبت شده است.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (response.StatusCode ==
                    HttpStatusCode.BadRequest)
                {
                    ShowApiError(
                        responseText,
                        "اطلاعات وارد شده صحیح نیست.");

                    return;
                }

                ShowApiError(
                    responseText,
                    "خطا در ثبت نام.");
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    "ارتباط با Nexa.Api برقرار نشد.\n\n" +
                    "آدرس API:\n" +
                    ApiBaseUrl +
                    "\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show(
                    "درخواست به سرور بیش از حد طول کشید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ثبت نام:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnCreate.Enabled =
                    true;

                Cursor =
                    Cursors.Default;
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
                            : message,
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                document.Dispose();

                MessageBox.Show(
                    defaultMessage +
                    "\n\n" +
                    responseText,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                MessageBox.Show(
                    defaultMessage +
                    "\n\n" +
                    responseText,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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

        private void txtFirstAndLastName_KeyPress(
            object sender,
            KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtPhoneNumber_KeyPress(
            object sender,
            KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) &&
                !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}