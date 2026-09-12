using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Nexa
{
    public partial class SavedMessagesForm : Form
    {
        private string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        private int currentUserId;

        public SavedMessagesForm(int userId)
        {
            InitializeComponent();

            currentUserId = userId;

            LoadSavedMessages();
        }

        private void LoadSavedMessages()
        {
            listSavedMessages.Items.Clear();

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            SM.Id,
                            SM.UserId,
                            SM.MessageId,
                            YM.YourId,
                            YM.AnswerId,
                            YM.YourMessage,
                            YM.AnswerMessage,
                            YM.MessageType,
                            YM.SentAt,
                            SM.SavedAt
                        FROM SavedMessages SM
                        INNER JOIN YourMessages YM
                            ON SM.MessageId = YM.Id
                        WHERE SM.UserId = @UserId
                        ORDER BY SM.SavedAt DESC;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SavedMessage savedMessage =
                                    new SavedMessage();

                                savedMessage.Id =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                savedMessage.UserId =
                                    Convert.ToInt32(
                                        reader["UserId"]);

                                savedMessage.MessageId =
                                    Convert.ToInt32(
                                        reader["MessageId"]);

                                string yourMessage =
                                    reader["YourMessage"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader["YourMessage"].ToString();

                                string answerMessage =
                                    reader["AnswerMessage"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader["AnswerMessage"].ToString();

                                /*
                                 * اگر پیام توسط کاربر فعلی ارسال شده
                                 * YourMessage را نمایش می‌دهیم.
                                 *
                                 * اگر پیام دریافتی باشد
                                 * AnswerMessage را نمایش می‌دهیم.
                                 */
                                if (!string.IsNullOrWhiteSpace(
                                    yourMessage))
                                {
                                    savedMessage.MessageText =
                                        yourMessage;
                                }
                                else
                                {
                                    savedMessage.MessageText =
                                        answerMessage;
                                }

                                string messageType =
                                    reader["MessageType"] ==
                                    DBNull.Value
                                    ? "Text"
                                    : reader["MessageType"].ToString();

                                if (string.IsNullOrWhiteSpace(
                                    savedMessage.MessageText))
                                {
                                    savedMessage.MessageText =
                                        "[" +
                                        messageType +
                                        "]";
                                }

                                savedMessage.SavedAt =
                                    Convert.ToDateTime(
                                        reader["SavedAt"]);

                                listSavedMessages.Items.Add(
                                    savedMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بارگذاری پیام‌های ذخیره‌شده:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SavedMessagesForm_Load(
            object sender,
            EventArgs e)
        {
            LoadSavedMessages();
        }

        private void btnDelete_Click(
            object sender,
            EventArgs e)
        {
            if (listSavedMessages.SelectedItem == null)
                return;

            SavedMessage savedMessage =
                listSavedMessages.SelectedItem
                as SavedMessage;

            if (savedMessage == null)
                return;

            DialogResult result =
                MessageBox.Show(
                    "آیا می‌خواهید این پیام از پیام‌های ذخیره‌شده حذف شود؟",
                    "Nexa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        DELETE FROM SavedMessages
                        WHERE Id = @Id
                          AND UserId = @UserId;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@Id",
                            SqlDbType.Int).Value =
                            savedMessage.Id;

                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        command.ExecuteNonQuery();
                    }
                }

                listSavedMessages.Items.Remove(
                    savedMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در حذف پیام:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCopy_Click(
            object sender,
            EventArgs e)
        {
            if (listSavedMessages.SelectedItem == null)
                return;

            SavedMessage savedMessage =
                listSavedMessages.SelectedItem
                as SavedMessage;

            if (savedMessage == null)
                return;

            if (string.IsNullOrWhiteSpace(
                savedMessage.MessageText))
                return;

            Clipboard.SetText(
                savedMessage.MessageText);

            MessageBox.Show(
                "پیام کپی شد.",
                "Nexa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnOpen_Click(
            object sender,
            EventArgs e)
        {
            if (listSavedMessages.SelectedItem == null)
                return;

            SavedMessage savedMessage =
                listSavedMessages.SelectedItem
                as SavedMessage;

            if (savedMessage == null)
                return;

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            YourId,
                            AnswerId,
                            YourMessage,
                            AnswerMessage,
                            MessageType,
                            SentAt
                        FROM YourMessages
                        WHERE Id = @MessageId;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@MessageId",
                            SqlDbType.Int).Value =
                            savedMessage.MessageId;

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show(
                                    "پیام اصلی پیدا نشد.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }

                            string yourMessage =
                                reader["YourMessage"] ==
                                DBNull.Value
                                ? ""
                                : reader["YourMessage"].ToString();

                            string answerMessage =
                                reader["AnswerMessage"] ==
                                DBNull.Value
                                ? ""
                                : reader["AnswerMessage"].ToString();

                            string message =
                                !string.IsNullOrWhiteSpace(
                                    yourMessage)
                                ? yourMessage
                                : answerMessage;

                            string messageType =
                                reader["MessageType"] ==
                                DBNull.Value
                                ? "Text"
                                : reader["MessageType"].ToString();

                            DateTime sentAt =
                                reader["SentAt"] ==
                                DBNull.Value
                                ? DateTime.MinValue
                                : Convert.ToDateTime(
                                    reader["SentAt"]);

                            MessageBox.Show(
                                "نوع پیام: " +
                                messageType +
                                "\n\n" +
                                message +
                                "\n\n" +
                                "زمان: " +
                                sentAt.ToString(
                                    "HH:mm  yyyy/MM/dd"),
                                "پیام ذخیره‌شده",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در باز کردن پیام:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Hide();
            frmLogin frmLogin = new frmLogin();
            frmLogin.ShowDialog();
        }
    }
}