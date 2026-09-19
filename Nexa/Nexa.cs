using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data;
using System.Speech.Recognition;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Json;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using OpenCvSharp;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class Nexa : Form
    {
        private SpeechRecognitionEngine recognizer;
        private bool isListening = false;
        private int selectedGroupId = 0;
        private ChatMessage replyToMessage;

        private Panel replyPanel;
        private Label replySenderLabel;
        private Label replyTextLabel;
        private Button replyCancelButton;
        private string userPhone;
        private readonly object videoLock = new object();
        private WaveInEvent waveIn;
        private HubConnection callConnection;
        private WaveFileWriter waveWriter;
        private string voiceFilePath;
        private readonly HttpClient apiClient =new HttpClient();
        private int currentUserId = 0;
        private WaveOutEvent voicePlayer;
        private WaveFileReader voiceReader;
        private string playingVoiceFile;
        private string currentUserYourID = "";
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private OpenCvSharp.VideoWriter videoWriter;
        private string videoMessageFilePath;
        private bool isRecordingVideo = false;
        private Timer videoMessageTimer;
        private int videoSecondsRemaining = 20;
        private Form videoPreviewForm;
        private PictureBox videoPreviewPictureBox;
        private Label videoTimerLabel;
        private int selectedUserId = 0;
        private string connectionString =@"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";
        private int lastMessageId = 0;
        public Nexa()
        {
            InitializeComponent();
            apiClient.BaseAddress =new Uri("https://localhost:7264/");
            timer1.Interval = 1000;
            timer1.Tick += timer1_Tick;
            timer1.Start();
            currentUserId = CurrentUser.Id;
            userPhone = CurrentUser.PhoneNumber;
        }
        public Nexa(string phoneNumber)
        {
            InitializeComponent();
            userPhone = phoneNumber;
            if (CurrentUser.Id > 0)
            {
                currentUserId = CurrentUser.Id;
                userPhone = CurrentUser.PhoneNumber;
            }
        }
        private async void Nexa_Load(object sender, EventArgs e)
        {

            try
            {
                if (currentUserId == 0 &&
                    !string.IsNullOrWhiteSpace(userPhone))
                {
                    GetCurrentUser();
                }

                if (currentUserId == 0)
                {
                    MessageBox.Show(
                        "کاربر فعلی شناسایی نشد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                CreateMessageContextMenu();
                CreateReplyPanel();
                await LoadChatHistoryFromDatabase();

                LoadStories();

                try
                {
                    recognizer =
                        new SpeechRecognitionEngine(
                            new CultureInfo("en-US"));

                    recognizer.SetInputToDefaultAudioDevice();

                    GrammarBuilder grammarBuilder =
                        new GrammarBuilder();

                    grammarBuilder.AppendDictation();

                    Grammar grammar =
                        new Grammar(grammarBuilder);

                    recognizer.LoadGrammar(grammar);

                    recognizer.SpeechRecognized +=
                        Recognizer_SpeechRecognized;

                    recognizer.RecognizeCompleted +=
                        Recognizer_RecognizeCompleted;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در راه‌اندازی تایپ صوتی:\n\n" +
                        ex.Message,
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                listAnswer.DrawMode =
                    DrawMode.OwnerDrawFixed;

                listAnswer.ItemHeight = 35;

                timer1.Interval = 1000;

                timer1.Start();

                await ConnectToCallServer();

                listHistory.MouseDown -=
                    listHistory_MouseDown;

                listHistory.MouseDown +=
                    listHistory_MouseDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در اجرای Nexa:\n\n" +
                    ex.ToString(),
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void Recognizer_SpeechRecognized(object sender,SpeechRecognizedEventArgs e)
        {
            if (e.Result == null)
                return;

            if (e.Result.Confidence < 0.50)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    txtYourMessage.AppendText(e.Result.Text + " ");
                }));

                return;
            }

            txtYourMessage.AppendText(e.Result.Text + " ");
        }
        private void Recognizer_RecognizeCompleted(object sender,RecognizeCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    isListening = false;
                }));
                return;
            }
            isListening = false;
        }
        private async void timer1_Tick(object sender,EventArgs e)
        {
            LoadNewMessages();
            UpdateMyLastSeen();
            if (selectedUserId > 0)
            {
                CheckSelectedUserStatus();
            }
            RefreshReadStatuses();
            await ConnectToCallServer();
        }
        private void GetCurrentUser()
        {
            try
            {
                if (CurrentUser.Id > 0)
                {
                    currentUserId = CurrentUser.Id;
                    using (SqlConnection con =new SqlConnection(connectionString))
                    {
                        string query = @"
                            SELECT
                                Id,
                                YourID,
                                PhoneNumber
                            FROM Users
                            WHERE Id = @Id";

                        using (SqlCommand cmd =new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add(
                                "@Id",
                                SqlDbType.Int).Value =
                                currentUserId;

                            con.Open();

                            using (SqlDataReader reader =
                                   cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    currentUserId =
                                        Convert.ToInt32(
                                            reader["Id"]);

                                    currentUserYourID =
                                        reader["YourID"] ==
                                        DBNull.Value
                                        ? ""
                                        : reader["YourID"]
                                            .ToString();

                                    if (reader["PhoneNumber"] !=
                                        DBNull.Value)
                                    {
                                        userPhone =
                                            reader["PhoneNumber"]
                                            .ToString();
                                    }
                                }
                            }
                        }
                    }

                    return;
                }

                if (string.IsNullOrWhiteSpace(userPhone))
                    return;

                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            Id,
                            YourID,
                            PhoneNumber
                        FROM Users
                        WHERE PhoneNumber = @PhoneNumber";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@PhoneNumber",
                            SqlDbType.NVarChar,
                            50).Value =
                            userPhone;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentUserId =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                currentUserYourID =
                                    reader["YourID"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader["YourID"]
                                        .ToString();

                                CurrentUser.Id =
                                    currentUserId;

                                if (reader["PhoneNumber"] !=
                                    DBNull.Value)
                                {
                                    userPhone =
                                        reader["PhoneNumber"]
                                        .ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در شناسایی کاربر:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private bool IsUserBlocked(int otherUserId)
        {
            if (currentUserId <= 0 ||
                otherUserId <= 0)
                return false;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT COUNT(1)
                        FROM UserBanList
                        WHERE UserId = @UserId
                        AND BannedUserId = @BannedUserId";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@BannedUserId",
                            SqlDbType.Int).Value =
                            otherUserId;

                        con.Open();

                        int count =
                            Convert.ToInt32(
                                cmd.ExecuteScalar());

                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CanMessageSelectedUser()
        {
            if (selectedUserId <= 0)
                return false;

            if (IsUserBlocked(selectedUserId))
            {
                MessageBox.Show(
                    "این کاربر بلاک شده است و امکان ارسال پیام به او وجود ندارد.",
                    "کاربر بلاک شده",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }
        private void myInformationToolStripMenuItem_Click(
            object sender,
            EventArgs e)
        {
            Hide();

            MyInformation information =
                new MyInformation();

            information.ShowDialog();

            Show();
        }
        private void txtIdSearch_TextChanged(
            object sender,
            EventArgs e)
        {
            SearchYourID(txtIdSearch.Text);
        }

        private void SearchYourID(string text)
        {
            lstResults.Items.Clear();

            if (string.IsNullOrWhiteSpace(text))
            {
                lstResults.Visible = false;
                return;
            }

            if (currentUserId == 0)
            {
                lstResults.Visible = false;
                return;
            }

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT YourID
                        FROM Users
                        WHERE YourID LIKE @Search + '%'
                        AND Id <> @CurrentUserId
                        ORDER BY YourID";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@Search",
                            SqlDbType.NVarChar,
                            50).Value =
                            text.Trim();

                        cmd.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lstResults.Items.Add(
                                    reader["YourID"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در جستجوی کاربر:\n\n" +
                    ex.Message);
            }

            lstResults.Visible =
                lstResults.Items.Count > 0;
        }

        private async void lstResults_DoubleClick(
     object sender,
     EventArgs e)
        {
            if (lstResults.SelectedItem == null)
                return;

            string yourID =
                lstResults.SelectedItem
                .ToString()
                .Trim();

            if (string.IsNullOrWhiteSpace(yourID))
                return;

            int otherUserId =
                GetUserIdByYourID(yourID);

            if (otherUserId == 0)
            {
                MessageBox.Show(
                    "کاربر پیدا نشد.");

                return;
            }

            if (IsUserBlocked(otherUserId))
            {
                MessageBox.Show(
                    "این کاربر بلاک شده است و امکان ارسال پیام به او وجود ندارد.");

                txtIdSearch.Clear();
                lstResults.Items.Clear();
                lstResults.Visible = false;

                return;
            }

            // ذخیره در دیتابیس
            SaveChatHistory(otherUserId);

            // گرفتن اطلاعات کاربر از SQL
            ChatHistoryItem selectedItem =
                GetUserForChatHistory(otherUserId);

            if (selectedItem == null)
            {
                MessageBox.Show(
                    "اطلاعات کاربر دریافت نشد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // بررسی اینکه قبلاً داخل لیست نباشد
            ChatHistoryItem existingItem = null;

            foreach (ChatHistoryItem item
                     in listHistory.Items)
            {
                if (item.UserId == otherUserId)
                {
                    existingItem = item;
                    break;
                }
            }

            // اگر نبود اضافه کن
            if (existingItem == null)
            {
                listHistory.Items.Add(selectedItem);
                existingItem = selectedItem;
            }

            // انتخاب کاربر
            listHistory.SelectedItem =
                existingItem;

            selectedUserId =
                existingItem.UserId;

            label3.Text =
                existingItem.FirstAndLastName;

            // بارگذاری پیام‌های این کاربر
            LoadConversation();

            // خواندن پیام‌ها
            MarkMessagesAsRead();

            // آخرین پیام
            UpdateLastMessageId();

            // پاک کردن جستجو
            txtIdSearch.Clear();
            lstResults.Items.Clear();
            lstResults.Visible = false;

            UpdateEmptyMessagePanels();

            listHistory.Refresh();
            listAnswer.Refresh();

            await Task.CompletedTask;
        }
        private int GetUserIdByYourID(string yourID)
        {
            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT Id
                        FROM Users
                        WHERE YourID = @YourID";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@YourID",
                            SqlDbType.NVarChar,
                            50).Value =
                            yourID;

                        con.Open();

                        object result =
                            cmd.ExecuteScalar();

                        if (result != null &&
                            result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در پیدا کردن کاربر:\n\n" +
                    ex.Message);
            }

            return 0;
        }
        private void SaveChatHistory(int otherUserId)
        {
            if (currentUserId <= 0 ||
                otherUserId <= 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        IF NOT EXISTS
                        (
                            SELECT 1
                            FROM ChatHistory
                            WHERE UserId = @UserId
                            AND OtherUserId = @OtherUserId
                        )
                        BEGIN
                            INSERT INTO ChatHistory
                            (
                                UserId,
                                OtherUserId
                            )
                            VALUES
                            (
                                @UserId,
                                @OtherUserId
                            )
                        END";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@OtherUserId",
                            SqlDbType.Int).Value =
                            otherUserId;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ذخیره تاریخچه:\n\n" +
                    ex.Message);
            }
        }
        private async Task LoadChatHistory()
        {
            listHistory.Items.Clear();

            if (currentUserId <= 0)
                return;

            try
            {
                List<ApiChatItem> items =
                    await GetChatHistoryFromApi(currentUserId);

                foreach (ApiChatItem item in items)
                {
                    // =========================
                    // کاربر
                    // =========================

                    if (item.Type == "User")
                    {
                        ChatHistoryItem chatItem =
                            new ChatHistoryItem();

                        chatItem.UserId =
                            item.UserId;

                        chatItem.YourID =
                            item.YourID ?? "";

                        chatItem.FirstAndLastName =
                            item.FirstAndLastName ?? "";

                        chatItem.IsBlocked =
                            item.IsBlocked;

                        listHistory.Items.Add(chatItem);
                    }

                    // =========================
                    // گروه
                    // =========================

                    else if (item.Type == "Group")
                    {
                        GroupInfo group =
                            new GroupInfo();

                        group.Id =
                            item.Id;

                        group.GroupName =
                            item.GroupName ?? "";

                        group.GroupBio =
                            item.GroupBio ?? "";

                        group.CreatedBy =
                            item.CreatedBy;

                        group.InviteCode =
                            item.InviteCode ?? "";

                        // عکس گروه
                        if (!string.IsNullOrWhiteSpace(
                            item.GroupPhoto))
                        {
                            try
                            {
                                group.GroupPhoto =
                                    Convert.FromBase64String(
                                        item.GroupPhoto);
                            }
                            catch
                            {
                                group.GroupPhoto = null;
                            }
                        }
                        else
                        {
                            group.GroupPhoto = null;
                        }

                        listHistory.Items.Add(group);
                    }
                }

                listHistory.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بارگذاری تاریخچه از API:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private ChatHistoryItem GetUserForChatHistory(int userId)
        {
            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT
                    Id,
                    YourID,
                    FirstAndLastName
                FROM Users
                WHERE Id = @Id";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@Id",
                            SqlDbType.Int).Value =
                            userId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ChatHistoryItem item =
                                    new ChatHistoryItem();

                                item.UserId =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                item.YourID =
                                    reader["YourID"]?.ToString()
                                    ?? "";

                                item.FirstAndLastName =
                                    reader["FirstAndLastName"]?.ToString()
                                    ?? "";

                                item.IsBlocked =
                                    IsUserBlocked(userId);

                                return item;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در دریافت اطلاعات کاربر:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return null;
        }
        private void listHistory_DoubleClick_2(
       object sender,
       EventArgs e)
        {
            if (listHistory.SelectedItem == null)
                return;

            try
            {
                GroupInfo group =
                    listHistory.SelectedItem as GroupInfo;

                if (group != null)
                {
                    if (group.Id <= 0)
                    {
                        MessageBox.Show(
                            "شناسه گروه نامعتبر است.",
                            "Nexa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    selectedGroupId = group.Id;

                    selectedUserId = 0;

                    label3.Text = group.GroupName;

                    try
                    {
                        LoadGroupMessages(selectedGroupId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "خطا در بارگذاری پیام‌های گروه:\n\n" +
                            ex.Message,
                            "Nexa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return;
                    }

                    try
                    {
                        UpdateEmptyMessagePanels();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "خطا در بروزرسانی پنل پیام‌ها:\n\n" +
                            ex.Message,
                            "Nexa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return;
                    }

                    listAnswer.Refresh();

                    return;
                }

                ChatHistoryItem item =
                    listHistory.SelectedItem
                    as ChatHistoryItem;

                if (item == null ||
                    item.UserId <= 0)
                    return;

                if (item.IsBlocked ||
                    IsUserBlocked(item.UserId))
                {
                    MessageBox.Show(
                        "این کاربر بلاک شده است و امکان ارسال پیام به او وجود ندارد.");

                    return;
                }

                selectedGroupId = 0;

                selectedUserId =
                    item.UserId;

                label3.Text =
                    item.FirstAndLastName;

                LoadConversation();

                MarkMessagesAsRead();
                CheckSelectedUserStatus();
                UpdateLastMessageId();

                UpdateEmptyMessagePanels();

                listAnswer.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در باز کردن گفتگو:\n\n" +
                    ex.Message +
                    "\n\nجزئیات:\n" +
                    ex.ToString(),
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void LoadGroupMessages(int groupId)
        {
            listAnswer.Items.Clear();

            if (groupId <= 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT
                    GM.Id,
                    GM.GroupId,
                    GM.SenderId,
                    U.FirstAndLastName,
                    U.YourID,
                    GM.MessageText,
                    GM.SentAt
                FROM GroupMessages GM
                INNER JOIN Users U
                    ON U.Id = GM.SenderId
                WHERE GM.GroupId = @GroupId
                ORDER BY GM.SentAt ASC";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            groupId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupMessage message =
                                    new GroupMessage();

                                message.Id =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                message.GroupId =
                                    Convert.ToInt32(
                                        reader["GroupId"]);

                                message.SenderId =
                                    Convert.ToInt32(
                                        reader["SenderId"]);

                                message.SenderName =
                                    reader["FirstAndLastName"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader["FirstAndLastName"].ToString();

                                message.SenderYourID =
                                    reader["YourID"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader["YourID"].ToString();

                                message.MessageText =
                                    reader["MessageText"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader["MessageText"].ToString();

                                message.SentAt =
                                    reader["SentAt"] ==
                                    DBNull.Value
                                    ? DateTime.Now
                                    : Convert.ToDateTime(
                                        reader["SentAt"]);

                                listAnswer.Items.Add(
                                    message);
                            }
                        }
                    }
                }

                if (listAnswer.Items.Count > 0)
                {
                    listAnswer.TopIndex =
                        listAnswer.Items.Count - 1;
                }

                listAnswer.Refresh();
            }
            catch (SqlException ex)
            {
                throw new Exception(
                    "خطای SQL در LoadGroupMessages:\n" +
                    ex.Message +
                    "\n\nشماره خطا: " +
                    ex.Number,
                    ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "خطا در LoadGroupMessages:\n" +
                    ex.Message,
                    ex);
            }
        }

        private void btnSend_Click(
       object sender,
       EventArgs e)
        {
            if (currentUserId == 0)
            {
                MessageBox.Show(
                    "کاربر فعلی شناسایی نشده است!");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtYourMessage.Text))
                return;

            if (selectedGroupId > 0)
            {
                SendGroupMessage();
                return;
            }

            if (selectedUserId == 0)
            {
                MessageBox.Show(
                    "ابتدا یک کاربر را انتخاب کنید!");

                return;
            }

            if (!CanMessageSelectedUser())
                return;

            string message =
                txtYourMessage.Text.Trim();

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"
INSERT INTO YourMessages
(
    YourId,
    YourMessage,
    AnswerId,
    AnswerMessage,
    ReplyToMessageId,
    IsRead,
    MessageType,
    SentAt
)
VALUES
(
    @YourId,
    @YourMessage,
    @AnswerId,
    @AnswerMessage,
    @ReplyToMessageId,
    @IsRead,
    'Text',
    GETDATE()
);

SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@YourId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@YourMessage",
                            SqlDbType.NVarChar,
                            -1).Value =
                            message;

                        cmd.Parameters.Add(
                            "@AnswerId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        cmd.Parameters.Add(
                            "@AnswerMessage",
                            SqlDbType.NVarChar,
                            -1).Value =
                            "";

                        cmd.Parameters.Add(
                            "@ReplyToMessageId",
                            SqlDbType.Int).Value =
                            replyToMessage != null
                            ? (object)replyToMessage.MessageId
                            : DBNull.Value;

                        cmd.Parameters.Add(
                            "@IsRead",
                            SqlDbType.Bit).Value =
                            false;

                        int messageId =
                            Convert.ToInt32(
                                cmd.ExecuteScalar());

                        ChatMessage newMessage =
                            new ChatMessage
                            {
                                MessageId = messageId,
                                SenderId = currentUserId,
                                Text = "شما: " + message,
                                IsRead = false,
                                SentAt = DateTime.Now,
                                MessageType = "Text",
                                ReplyToMessageId =
                                    replyToMessage != null
                                    ? (int?)replyToMessage.MessageId
                                    : null
                            };

                        if (replyToMessage != null)
                        {
                            newMessage.ReplyToText =
                                replyToMessage.Text;

                            newMessage.ReplyToSenderName =
                                replyToMessage.SenderId ==
                                currentUserId
                                ? "شما"
                                : label3.Text;
                        }

                        listAnswer.Items.Add(
                            newMessage);
                    }
                }

                txtYourMessage.Clear();

                replyToMessage = null;

                if (replyPanel != null)
                {
                    replyPanel.Visible = false;

                    replySenderLabel.Text = "";

                    replyTextLabel.Text = "";
                }

                if (listAnswer.Items.Count > 0)
                {
                    listAnswer.TopIndex =
                        listAnswer.Items.Count - 1;
                }

                listAnswer.Refresh();

                UpdateLastMessageId();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ارسال پیام:\n\n" +
                    ex.Message);
            }
        }
        private void SendGroupMessage()
        {
            if (selectedGroupId <= 0)
            {
                MessageBox.Show("گروهی انتخاب نشده است.");
                return;
            }

            string message = txtYourMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (currentUserId <= 0)
            {
                MessageBox.Show("کاربر فعلی شناسایی نشده است.");
                return;
            }

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                INSERT INTO GroupMessages
                (
                    GroupId,
                    SenderId,
                    MessageText,
                    MessageType
                )
                VALUES
                (
                    @GroupId,
                    @SenderId,
                    @MessageText,
                    @MessageType
                )";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            selectedGroupId;

                        cmd.Parameters.Add(
                            "@SenderId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@MessageText",
                            SqlDbType.NVarChar,
                            -1).Value =
                            message;

                        cmd.Parameters.Add(
                            "@MessageType",
                            SqlDbType.NVarChar,
                            30).Value =
                            "Text";

                        con.Open();

                        int result =
                            cmd.ExecuteNonQuery();

                        if (result <= 0)
                        {
                            MessageBox.Show(
                                "پیام در دیتابیس ثبت نشد.");
                            return;
                        }
                    }
                }

                txtYourMessage.Clear();

                LoadGroupMessages(selectedGroupId);

                if (listAnswer.Items.Count > 0)
                {
                    listAnswer.TopIndex =
                        listAnswer.Items.Count - 1;
                }

                listAnswer.Refresh();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "خطای SQL هنگام ارسال پیام گروه:\n\n" +
                    ex.Message +
                    "\n\nشماره خطا: " +
                    ex.Number,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ارسال پیام به گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadConversation()
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            try
            {
                listAnswer.Items.Clear();

                lastMessageId = 0;

                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
        SELECT
            Id,
            YourId,
            AnswerId,
            YourMessage,
            ReplyToMessageId,
            IsRead,
            SentAt,
            MessageType,
            VoiceData,
            FileName,
            FileData,
            ImageData,
            Latitude,
            Longitude,
            VideoData,
            GifData
        FROM YourMessages
        WHERE
        (
            YourId = @CurrentUserId
            AND AnswerId = @SelectedUserId
        )
        OR
        (
            YourId = @SelectedUserId
            AND AnswerId = @CurrentUserId
        )
        ORDER BY Id ASC";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@SelectedUserId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int messageId =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                int senderId =
                                    Convert.ToInt32(
                                        reader["YourId"]);

                                int? replyToMessageId = null;

                                if (reader["ReplyToMessageId"] !=
                                    DBNull.Value)
                                {
                                    replyToMessageId =
                                        Convert.ToInt32(
                                            reader["ReplyToMessageId"]);
                                }

                                string messageType =
                                    reader["MessageType"] ==
                                    DBNull.Value
                                    ? "Text"
                                    : reader["MessageType"]
                                        .ToString();

                                bool isRead =
                                    reader["IsRead"] !=
                                    DBNull.Value &&
                                    Convert.ToBoolean(
                                        reader["IsRead"]);

                                DateTime sentAt =
                                    reader["SentAt"] ==
                                    DBNull.Value
                                    ? DateTime.Now
                                    : Convert.ToDateTime(
                                        reader["SentAt"]);

                                ChatMessage chatMessage =
                                    new ChatMessage
                                    {
                                        MessageId =
                                            messageId,

                                        SenderId =
                                            senderId,

                                        IsRead =
                                            isRead,

                                        SentAt =
                                            sentAt,

                                        MessageType =
                                            messageType,

                                        ReplyToMessageId =
                                            replyToMessageId
                                    };

                                if (messageType == "Voice")
                                {
                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🎤 پیام صوتی"
                                        : label3.Text +
                                          ": 🎤 پیام صوتی";

                                    if (reader["VoiceData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.VoiceData =
                                            (byte[])reader[
                                                "VoiceData"];
                                    }
                                }

                                else if (messageType == "Video")
                                {
                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🎥 پیام ویدیویی"
                                        : label3.Text +
                                          ": 🎥 پیام ویدیویی";

                                    if (reader["VideoData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.VideoData =
                                            (byte[])reader[
                                                "VideoData"];
                                    }

                                    chatMessage.FileName =
                                        reader["FileName"] ==
                                        DBNull.Value
                                        ? "NexaVideo.avi"
                                        : reader["FileName"]
                                            .ToString();
                                }

                                else if (messageType == "GIF")
                                {
                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🎞️ GIF"
                                        : label3.Text +
                                          ": 🎞️ GIF";

                                    if (reader["GifData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.GifData =
                                            (byte[])reader[
                                                "GifData"];
                                    }

                                    chatMessage.FileName =
                                        reader["FileName"] ==
                                        DBNull.Value
                                        ? "NexaGif.gif"
                                        : reader["FileName"]
                                            .ToString();
                                }

                                else if (messageType == "File")
                                {
                                    string fileName =
                                        reader["FileName"] ==
                                        DBNull.Value
                                        ? "فایل"
                                        : reader["FileName"]
                                            .ToString();

                                    chatMessage.FileName =
                                        fileName;

                                    if (reader["FileData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.FileData =
                                            (byte[])reader[
                                                "FileData"];
                                    }

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 📎 " +
                                          fileName
                                        : label3.Text +
                                          ": 📎 " +
                                          fileName;
                                }

                                else if (messageType == "Image")
                                {
                                    if (reader["ImageData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.ImageData =
                                            (byte[])reader[
                                                "ImageData"];
                                    }

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🖼️ عکس"
                                        : label3.Text +
                                          ": 🖼️ عکس";
                                }

                                else if (messageType == "Location")
                                {
                                    if (reader["Latitude"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.Latitude =
                                            Convert.ToDouble(
                                                reader[
                                                    "Latitude"]);
                                    }

                                    if (reader["Longitude"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.Longitude =
                                            Convert.ToDouble(
                                                reader[
                                                    "Longitude"]);
                                    }

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 📍 موقعیت مکانی"
                                        : label3.Text +
                                          ": 📍 موقعیت مکانی";
                                }

                                else
                                {
                                    string message =
                                        reader["YourMessage"] ==
                                        DBNull.Value
                                        ? ""
                                        : reader[
                                            "YourMessage"]
                                            .ToString();

                                    if (string.IsNullOrWhiteSpace(
                                        message))
                                        continue;

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: " + message
                                        : label3.Text +
                                          ": " + message;
                                }

                                if (chatMessage.ReplyToMessageId.HasValue)
                                {
                                    foreach (ChatMessage oldMessage
                                             in listAnswer.Items)
                                    {
                                        if (oldMessage.MessageId ==
                                            chatMessage.ReplyToMessageId.Value)
                                        {
                                            chatMessage.ReplyToText =
                                                oldMessage.Text;

                                            chatMessage.ReplyToSenderName =
                                                oldMessage.SenderId ==
                                                currentUserId
                                                ? "شما"
                                                : label3.Text;

                                            break;
                                        }
                                    }
                                }

                                LoadReactionForMessage(
                                    chatMessage);

                                listAnswer.Items.Add(
                                    chatMessage);

                                if (messageId >
                                    lastMessageId)
                                {
                                    lastMessageId =
                                        messageId;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بارگذاری گفتگو:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void LoadReactionForMessage(
        ChatMessage message)
        {
            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT
                    ReactionType,
                    COUNT(*) AS ReactionCount
                FROM MessageReactions
                WHERE MessageId = @MessageId
                GROUP BY ReactionType
                ORDER BY MIN(CreatedAt);";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@MessageId",
                            SqlDbType.Int).Value =
                            message.MessageId;

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            List<string> reactions =
                                new List<string>();

                            int totalCount = 0;

                            while (reader.Read())
                            {
                                string reaction =
                                    reader["ReactionType"].ToString();

                                int count =
                                    Convert.ToInt32(
                                        reader["ReactionCount"]);

                                if (count > 1)
                                {
                                    reactions.Add(
                                        reaction + " " + count);
                                }
                                else
                                {
                                    reactions.Add(
                                        reaction);
                                }

                                totalCount += count;
                            }

                            message.Reaction =
                                string.Join(
                                    "  ",
                                    reactions);

                            message.ReactionCount =
                                totalCount;
                        }
                    }
                }
            }
            catch
            {
                message.Reaction = null;
                message.ReactionCount = 0;
            }
        }

        private void MarkMessagesAsRead()
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        UPDATE YourMessages
                        SET IsRead = 1
                        WHERE YourId = @SelectedUserId
                        AND AnswerId = @CurrentUserId
                        AND IsRead = 0";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@SelectedUserId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        cmd.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadNewMessages()
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            Id,
                            YourId,
                            YourMessage,
                            IsRead,
                            SentAt,
                            MessageType,
                            VoiceData,
                            FileName,
                            FileData,
                            ImageData,
                            Latitude,
                            Longitude,
                            VideoData
                        FROM YourMessages
                        WHERE Id > @LastMessageId
                        AND
                        (
                            (
                                YourId = @SelectedUserId
                                AND AnswerId = @CurrentUserId
                            )
                            OR
                            (
                                YourId = @CurrentUserId
                                AND AnswerId = @SelectedUserId
                            )
                        )
                        ORDER BY Id";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@LastMessageId",
                            SqlDbType.Int).Value =
                            lastMessageId;

                        cmd.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@SelectedUserId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int messageId =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                int senderId =
                                    Convert.ToInt32(
                                        reader["YourId"]);

                                string messageType =
                                    reader["MessageType"] ==
                                    DBNull.Value
                                    ? "Text"
                                    : reader["MessageType"]
                                        .ToString();

                                bool isRead =
                                    reader["IsRead"] !=
                                    DBNull.Value &&
                                    Convert.ToBoolean(
                                        reader["IsRead"]);

                                DateTime sentAt =
                                    reader["SentAt"] ==
                                    DBNull.Value
                                    ? DateTime.Now
                                    : Convert.ToDateTime(
                                        reader["SentAt"]);
                                ChatMessage chatMessage =
                                    new ChatMessage
                                    {
                                        MessageId = messageId,
                                        SenderId = senderId,
                                        IsRead = isRead,
                                        SentAt = sentAt,
                                        MessageType = messageType
                                    };

                                if (messageType == "Video")
                                {
                                    if (reader["VideoData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.VideoData =
                                            (byte[])reader[
                                                "VideoData"];
                                    }

                                    chatMessage.FileName =
                                        reader["FileName"] ==
                                        DBNull.Value
                                        ? "NexaVideo.avi"
                                        : reader["FileName"]
                                            .ToString();

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🎥 پیام ویدیویی"
                                        : label3.Text +
                                          ": 🎥 پیام ویدیویی";

                                    if (senderId !=
                                        currentUserId)
                                    {
                                        chatMessage.IsRead = true;
                                    }

                                    listAnswer.Items.Add(
                                        chatMessage);
                                }
                                else if (messageType == "Voice")
                                {
                                    if (reader["VoiceData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.VoiceData =
                                            (byte[])reader[
                                                "VoiceData"];
                                    }

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🎤 پیام صوتی"
                                        : label3.Text +
                                          ": 🎤 پیام صوتی";

                                    listAnswer.Items.Add(
                                        chatMessage);
                                }
                                else if (messageType == "File")
                                {
                                    if (reader["FileData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.FileData =
                                            (byte[])reader[
                                                "FileData"];
                                    }

                                    chatMessage.FileName =
                                        reader["FileName"] ==
                                        DBNull.Value
                                        ? "فایل"
                                        : reader["FileName"]
                                            .ToString();

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 📎 " +
                                          chatMessage.FileName
                                        : label3.Text +
                                          ": 📎 " +
                                          chatMessage.FileName;

                                    listAnswer.Items.Add(
                                        chatMessage);
                                }
                                else if (messageType == "Image")
                                {
                                    if (reader["ImageData"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.ImageData =
                                            (byte[])reader[
                                                "ImageData"];
                                    }

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 🖼️ عکس"
                                        : label3.Text +
                                          ": 🖼️ عکس";

                                    listAnswer.Items.Add(
                                        chatMessage);
                                }
                                else if (messageType == "Location")
                                {
                                    if (reader["Latitude"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.Latitude =
                                            Convert.ToDouble(
                                                reader[
                                                    "Latitude"]);
                                    }

                                    if (reader["Longitude"] !=
                                        DBNull.Value)
                                    {
                                        chatMessage.Longitude =
                                            Convert.ToDouble(
                                                reader[
                                                    "Longitude"]);
                                    }

                                    chatMessage.Text =
                                        senderId ==
                                        currentUserId
                                        ? "شما: 📍 موقعیت مکانی"
                                        : label3.Text +
                                          ": 📍 موقعیت مکانی";

                                    listAnswer.Items.Add(
                                        chatMessage);
                                }
                                else
                                {
                                    string message =
                                        reader["YourMessage"] ==
                                        DBNull.Value
                                        ? ""
                                        : reader[
                                            "YourMessage"]
                                            .ToString();

                                    if (!string.IsNullOrWhiteSpace(
                                        message))
                                    {
                                        chatMessage.Text =
                                            senderId ==
                                            currentUserId
                                            ? "شما: " + message
                                            : label3.Text +
                                              ": " + message;

                                        if (senderId !=
                                            currentUserId)
                                        {
                                            chatMessage.IsRead =
                                                true;
                                        }

                                        listAnswer.Items.Add(
                                            chatMessage);
                                    }
                                }

                                if (messageId >
                                    lastMessageId)
                                {
                                    lastMessageId =
                                        messageId;
                                }
                            }
                        }
                    }
                }

                if (listAnswer.Items.Count > 0)
                {
                    listAnswer.TopIndex =
                        listAnswer.Items.Count - 1;
                }

                listAnswer.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در LoadNewMessages:\n\n" +
                    ex.Message);
            }
        }

        private void RefreshReadStatuses()
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            try
            {
                Dictionary<int, bool> readStatuses =
                    new Dictionary<int, bool>();

                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT Id, IsRead
                        FROM YourMessages
                        WHERE YourId = @CurrentUserId
                        AND AnswerId = @SelectedUserId";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@SelectedUserId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                bool isRead =
                                    Convert.ToBoolean(
                                        reader["IsRead"]);

                                readStatuses[id] =
                                    isRead;
                            }
                        }
                    }
                }

                foreach (ChatMessage item
                         in listAnswer.Items)
                {
                    if (readStatuses.ContainsKey(
                        item.MessageId))
                    {
                        item.IsRead =
                            readStatuses[
                                item.MessageId];
                    }
                }

                listAnswer.Refresh();
            }
            catch
            {
            }
        }

        private void UpdateLastMessageId()
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT ISNULL(MAX(Id), 0)
                        FROM YourMessages
                        WHERE
                        (
                            YourId = @CurrentUserId
                            AND AnswerId = @SelectedUserId
                        )
                        OR
                        (
                            YourId = @SelectedUserId
                            AND AnswerId = @CurrentUserId
                        )";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@SelectedUserId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        con.Open();

                        object result =
                            cmd.ExecuteScalar();

                        if (result != null)
                        {
                            lastMessageId =
                                Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private async  void btnDeleteHistory_Click(
            object sender,
            EventArgs e)
        {
            if (listHistory.SelectedItem == null)
                return;

            if (listHistory.SelectedItem is GroupInfo group)
            {
                await ConfirmDeleteGroup(group);
            }
            if (listHistory.SelectedItem == null)
                return;

            ChatHistoryItem item =
                listHistory.SelectedItem
                as ChatHistoryItem;

            if (item == null)
                return;

            DialogResult result =
                MessageBox.Show(
                    "آیا مطمئن هستید که می‌خواهید این کاربر را از تاریخچه حذف کنید؟",
                    "تأیید حذف",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        DELETE FROM ChatHistory
                        WHERE UserId = @UserId
                        AND OtherUserId = @OtherUserId";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@OtherUserId",
                            SqlDbType.Int).Value =
                            item.UserId;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }

                listHistory.Items.Remove(item);

                if (selectedUserId ==
                    item.UserId)
                {
                    selectedUserId = 0;

                    label3.Text = "";

                    listAnswer.Items.Clear();

                    lastMessageId = 0;
                }

                UpdateEmptyMessagePanels();

                listAnswer.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در حذف تاریخچه:\n\n" +
                    ex.Message);
            }
        }

        private void btnAddStory_Click(
            object sender,
            EventArgs e)
        {
            if (currentUserId == 0)
            {
                MessageBox.Show(
                    "کاربر فعلی پیدا نشد!");

                return;
            }

            using (OpenFileDialog ofd =
                   new OpenFileDialog())
            {
                ofd.Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

                ofd.Title =
                    "انتخاب عکس استوری";

                if (ofd.ShowDialog() !=
                    DialogResult.OK)
                    return;

                try
                {
                    byte[] imageData =
                        File.ReadAllBytes(
                            ofd.FileName);

                    using (SqlConnection con =
                           new SqlConnection(connectionString))
                    {
                        string query = @"
                            INSERT INTO Stories
                            (
                                UserId,
                                StoryType,
                                StoryData,
                                StoryText,
                                CreatedAt,
                                ExpiresAt
                            )
                            VALUES
                            (
                                @UserId,
                                @StoryType,
                                @StoryData,
                                NULL,
                                GETDATE(),
                                DATEADD(HOUR, 24, GETDATE())
                            )";

                        using (SqlCommand cmd =
                               new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add(
                                "@UserId",
                                SqlDbType.Int).Value =
                                currentUserId;

                            cmd.Parameters.Add(
                                "@StoryType",
                                SqlDbType.NVarChar,
                                20).Value =
                                "Image";

                            cmd.Parameters.Add(
                                "@StoryData",
                                SqlDbType.VarBinary,
                                -1).Value =
                                imageData;

                            con.Open();

                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show(
                        "استوری با موفقیت اضافه شد.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در اضافه کردن استوری:\n\n" +
                        ex.Message);
                }
            }
        }

        private void LoadStories()
        {
            flowStories.Controls.Clear();

            if (currentUserId == 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            S.Id,
                            S.UserId,
                            S.StoryData,
                            U.YourID
                        FROM Stories S
                        INNER JOIN Users U
                            ON U.Id = S.UserId
                        WHERE S.ExpiresAt > GETDATE()
                        AND EXISTS
                        (
                            SELECT 1
                            FROM ChatHistory H
                            WHERE H.UserId = @UserId
                            AND H.OtherUserId = S.UserId
                        )
                        ORDER BY S.CreatedAt DESC";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        con.Open();

                        using (SqlDataReader reader =
                               cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int storyId =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                string yourID =
                                    reader["YourID"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader[
                                        "YourID"]
                                        .ToString();

                                if (reader["StoryData"] ==
                                    DBNull.Value)
                                    continue;

                                byte[] imageData =
                                    (byte[])reader[
                                        "StoryData"];

                                using (MemoryStream ms =
                                       new MemoryStream(
                                           imageData))
                                using (Image image =
                                       Image.FromStream(ms))
                                {
                                    PictureBox pictureBox =
                                        new PictureBox();

                                    pictureBox.Width = 90;
                                    pictureBox.Height = 90;
                                    pictureBox.SizeMode =
                                        PictureBoxSizeMode.Zoom;
                                    pictureBox.Image =
                                        new Bitmap(image);
                                    pictureBox.Tag =
                                        storyId;
                                    pictureBox.Cursor =
                                        Cursors.Hand;

                                    Label label =
                                        new Label();

                                    label.Text =
                                        yourID;

                                    label.AutoSize =
                                        false;

                                    label.Width = 90;
                                    label.Height = 25;

                                    label.TextAlign =
                                        ContentAlignment
                                        .MiddleCenter;

                                    pictureBox.DoubleClick +=
                                        StoryPictureBox_DoubleClick;

                                    Panel panel =
                                        new Panel();

                                    panel.Width = 100;
                                    panel.Height = 120;

                                    panel.Controls.Add(
                                        pictureBox);

                                    panel.Controls.Add(
                                        label);

                                    label.Top = 92;

                                    flowStories.Controls.Add(
                                        panel);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بارگذاری استوری‌ها:\n\n" +
                    ex.Message);
            }
        }

        private void StoryPictureBox_DoubleClick(
            object sender,
            EventArgs e)
        {
            if (sender is PictureBox pictureBox &&
                pictureBox.Tag != null)
            {
                int storyId =
                    Convert.ToInt32(
                        pictureBox.Tag);

                StoryViewer viewer =
                    new StoryViewer(storyId);

                viewer.ShowDialog();
            }
        }

        private void btnVoice_Click(
            object sender,
            EventArgs e)
        {
            if (waveIn == null)
            {
                if (currentUserId == 0 ||
                    selectedUserId == 0)
                {
                    MessageBox.Show(
                        "ابتدا یک کاربر را انتخاب کنید.");

                    return;
                }

                if (!CanMessageSelectedUser())
                    return;

                string fileName =
                    "Voice_" +
                    DateTime.Now.ToString(
                        "yyyyMMdd_HHmmss") +
                    ".wav";

                voiceFilePath =
                    Path.Combine(
                        Application.StartupPath,
                        fileName);

                waveIn =
                    new WaveInEvent();

                waveIn.WaveFormat =
                    new WaveFormat(44100, 1);

                waveWriter =
                    new WaveFileWriter(
                        voiceFilePath,
                        waveIn.WaveFormat);

                waveIn.DataAvailable +=
                    (s, a) =>
                    {
                        if (waveWriter != null)
                        {
                            waveWriter.Write(
                                a.Buffer,
                                0,
                                a.BytesRecorded);

                            waveWriter.Flush();
                        }
                    };

                waveIn.RecordingStopped +=
                    WaveIn_RecordingStopped;

                waveIn.StartRecording();

                btnVoice.Text = "⏹ توقف";
            }
            else
            {
                waveIn.StopRecording();

                btnVoice.Text = "🎤";
            }
        }

        private void WaveIn_RecordingStopped(
            object sender,
            StoppedEventArgs e)
        {
            try
            {
                if (waveWriter != null)
                {
                    waveWriter.Dispose();
                    waveWriter = null;
                }

                if (waveIn != null)
                {
                    waveIn.Dispose();
                    waveIn = null;
                }

                if (string.IsNullOrEmpty(
                    voiceFilePath) ||
                    !File.Exists(voiceFilePath))
                    return;

                if (currentUserId == 0 ||
                    selectedUserId == 0)
                    return;

                if (IsUserBlocked(selectedUserId))
                {
                    MessageBox.Show(
                        "این کاربر بلاک شده است و امکان ارسال پیام به او وجود ندارد.");

                    try
                    {
                        if (File.Exists(
                            voiceFilePath))
                        {
                            File.Delete(
                                voiceFilePath);
                        }
                    }
                    catch
                    {
                    }

                    voiceFilePath = null;

                    return;
                }

                byte[] voiceData =
                    File.ReadAllBytes(
                        voiceFilePath);

                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        INSERT INTO YourMessages
                        (
                            YourId,
                            YourMessage,
                            AnswerId,
                            AnswerMessage,
                            IsRead,
                            MessageType,
                            VoiceData,
                            SentAt
                        )
                        VALUES
                        (
                            @YourId,
                            '',
                            @AnswerId,
                            '',
                            0,
                            'Voice',
                            @VoiceData,
                            GETDATE()
                        )";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@YourId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@AnswerId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        cmd.Parameters.Add(
                            "@VoiceData",
                            SqlDbType.VarBinary,
                            -1).Value =
                            voiceData;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }

                try
                {
                    if (File.Exists(
                        voiceFilePath))
                    {
                        File.Delete(
                            voiceFilePath);
                    }
                }
                catch
                {
                }

                voiceFilePath = null;

                UpdateLastMessageId();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ذخیره پیام صوتی:\n\n" +
                    ex.Message);
            }
        }

        private void btnVoicemessage_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                if (isRecordingVideo)
                {
                    StopVideoRecording();
                    return;
                }
                if (!CanMessageSelectedUser())
                    return;
                StartVideoRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در پیام ویدیویی:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void StartVideoRecording()
        {
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    MessageBox.Show(
                        "هیچ دوربینی پیدا نشد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);

                int width = 640;
                int height = 480;

                if (videoSource.VideoCapabilities != null &&
                    videoSource.VideoCapabilities.Length > 0)
                {
                    var capability = videoSource.VideoCapabilities
                        .OrderByDescending(x => x.FrameSize.Width * x.FrameSize.Height)
                        .FirstOrDefault();

                    if (capability != null)
                    {
                        width = capability.FrameSize.Width;
                        height = capability.FrameSize.Height;
                    }
                }

                string tempFolder = Path.Combine(
                    Path.GetTempPath(),
                    "NexaVideos");

                Directory.CreateDirectory(tempFolder);

                videoMessageFilePath = Path.Combine(
                    tempFolder,
                    "NexaVideo_" + Guid.NewGuid().ToString("N") + ".avi");

                videoWriter = new OpenCvSharp.VideoWriter(
                    videoMessageFilePath,
                    OpenCvSharp.FourCC.MJPG,
                    20,
                    new OpenCvSharp.Size(width, height));

                if (!videoWriter.IsOpened())
                {
                    videoWriter.Dispose();
                    videoWriter = null;

                    MessageBox.Show(
                        "امکان ایجاد فایل ویدیو وجود ندارد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                videoSource.NewFrame += VideoSource_NewFrame;

                videoSource.Start();

                isRecordingVideo = true;
                videoSecondsRemaining = 20;

                ShowVideoPreview();

                videoMessageTimer = new Timer();
                videoMessageTimer.Interval = 1000;

                videoMessageTimer.Tick += (s, e) =>
                {
                    videoSecondsRemaining--;

                    if (videoTimerLabel != null &&
                        !videoTimerLabel.IsDisposed)
                    {
                        videoTimerLabel.Text =
                            "زمان باقی‌مانده: " +
                            videoSecondsRemaining +
                            " ثانیه";
                    }

                    if (videoSecondsRemaining <= 0)
                    {
                        StopVideoRecording();
                    }
                };

                videoMessageTimer.Start();
            }
            catch (Exception ex)
            {
                CleanupVideoResources();

                MessageBox.Show(
                    "خطا در شروع ضبط ویدیو:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    if (videoWriter != null && videoWriter.IsOpened())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitmap.Save(
                                ms,
                                System.Drawing.Imaging.ImageFormat.Bmp);

                            byte[] imageBytes = ms.ToArray();

                            using (Mat frame = Cv2.ImDecode(
                                imageBytes,
                                ImreadModes.Color))
                            {
                                if (!frame.Empty())
                                {
                                    lock (videoLock)
                                    {
                                        if (videoWriter != null &&
                                            videoWriter.IsOpened())
                                        {
                                            videoWriter.Write(frame);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (videoPreviewPictureBox != null &&
                        !videoPreviewPictureBox.IsDisposed)
                    {
                        Bitmap preview = (Bitmap)bitmap.Clone();

                        videoPreviewPictureBox.BeginInvoke(
                            new Action(() =>
                            {
                                if (videoPreviewPictureBox.IsDisposed)
                                {
                                    preview.Dispose();
                                    return;
                                }

                                Image oldImage = videoPreviewPictureBox.Image;
                                videoPreviewPictureBox.Image = preview;

                                if (oldImage != null)
                                    oldImage.Dispose();
                            }));
                    }
                }
            }
            catch
            {
                // جلوگیری از Crash شدن برنامه هنگام بسته شدن دوربین
            }
        }

        private void ShowVideoPreview()
        {
            videoPreviewForm = new Form();

            videoPreviewForm.Text = "Nexa - Video Message";
            videoPreviewForm.StartPosition = FormStartPosition.CenterScreen;
            videoPreviewForm.Size = new System.Drawing.Size(700, 580);
            videoPreviewForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            videoPreviewForm.MaximizeBox = false;

            videoPreviewPictureBox = new PictureBox();

            videoPreviewPictureBox.Dock = DockStyle.Fill;
            videoPreviewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            videoPreviewPictureBox.BackColor = Color.Black;

            videoTimerLabel = new Label();

            videoTimerLabel.Text = "زمان باقی‌مانده: 20 ثانیه";
            videoTimerLabel.Dock = DockStyle.Top;
            videoTimerLabel.Height = 45;
            videoTimerLabel.TextAlign = ContentAlignment.MiddleCenter;
            videoTimerLabel.Font = new Font("Arial", 14, FontStyle.Bold);
            videoTimerLabel.ForeColor = Color.White;
            videoTimerLabel.BackColor = Color.Black;

            videoPreviewForm.Controls.Add(videoPreviewPictureBox);
            videoPreviewForm.Controls.Add(videoTimerLabel);

            videoPreviewForm.FormClosing += VideoPreviewForm_FormClosing;

            videoPreviewForm.Show();
        }

        private void VideoMessageTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (!isRecordingVideo)
                return;

            videoSecondsRemaining--;

            if (videoSecondsRemaining < 0)
                videoSecondsRemaining = 0;

            if (videoTimerLabel != null &&
                !videoTimerLabel.IsDisposed)
            {
                videoTimerLabel.Text =
                    "00:" +
                    videoSecondsRemaining
                    .ToString("00");
            }

            if (videoSecondsRemaining <= 0)
            {
                StopVideoRecording();
            }
        }

        private void VideoPreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRecordingVideo)
            {
                e.Cancel = true;
                StopVideoRecording();
            }
        }

        private void StopVideoRecording()
        {
            if (!isRecordingVideo)
                return;

            isRecordingVideo = false;

            try
            {
                if (videoMessageTimer != null)
                {
                    videoMessageTimer.Stop();
                    videoMessageTimer.Dispose();
                    videoMessageTimer = null;
                }

                if (videoSource != null)
                {
                    videoSource.NewFrame -= VideoSource_NewFrame;

                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        videoSource.WaitForStop();
                    }

                    videoSource = null;
                }

                lock (videoLock)
                {
                    if (videoWriter != null)
                    {
                        videoWriter.Release();
                        videoWriter.Dispose();
                        videoWriter = null;
                    }
                }

                if (videoPreviewForm != null &&
                    !videoPreviewForm.IsDisposed)
                {
                    videoPreviewForm.FormClosing -= VideoPreviewForm_FormClosing;
                    videoPreviewForm.Close();
                    videoPreviewForm.Dispose();
                }

                videoPreviewForm = null;
                videoPreviewPictureBox = null;
                videoTimerLabel = null;

                if (string.IsNullOrEmpty(videoMessageFilePath) ||
                    !File.Exists(videoMessageFilePath))
                {
                    MessageBox.Show(
                        "ویدیویی برای ارسال ایجاد نشد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                FileInfo fileInfo = new FileInfo(videoMessageFilePath);

                if (fileInfo.Length < 1000)
                {
                    File.Delete(videoMessageFilePath);

                    MessageBox.Show(
                        "ضبط ویدیو ناموفق بود.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (!CanMessageSelectedUser())
                {
                    File.Delete(videoMessageFilePath);
                    videoMessageFilePath = null;
                    return;
                }

                DialogResult result = MessageBox.Show(
                    "مطمئنی می‌خواهی این ویدیو را ارسال کنی؟",
                    "ارسال پیام ویدیویی",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SendVideoMessage(videoMessageFilePath);
                }
                else
                {
                    File.Delete(videoMessageFilePath);
                }

                videoMessageFilePath = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در توقف ضبط:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void SendVideoMessage(string filePath)
        {
            try
            {
                if (!CanMessageSelectedUser())
                    return;

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                byte[] videoData = File.ReadAllBytes(filePath);

                if (videoData.Length == 0)
                    return;

                int messageId;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO YourMessages
                (
                    YourId,
                    YourMessage,
                    AnswerId,
                    AnswerMessage,
                    IsRead,
                    SentAt,
                    MessageType,
                    VideoData,
                    FileName
                )
                VALUES
                (
                    @YourId,
                    '',
                    @AnswerId,
                    '',
                    0,
                    GETDATE(),
                    'Video',
                    @VideoData,
                    @FileName
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@YourId", currentUserId);
                        command.Parameters.AddWithValue("@AnswerId", selectedUserId);
                        command.Parameters.Add("@VideoData", SqlDbType.VarBinary, -1)
                            .Value = videoData;
                        command.Parameters.AddWithValue(
                            "@FileName",
                            Path.GetFileName(filePath));

                        messageId = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                ChatMessage message = new ChatMessage
                {
                    MessageId = messageId,
                    Text = "شما: 🎥 پیام ویدیویی",
                    IsRead = false,
                    SentAt = DateTime.Now,
                    MessageType = "Video",
                    VideoData = videoData,
                    FileName = Path.GetFileName(filePath)
                };

                listAnswer.Items.Add(message);
                listAnswer.TopIndex = listAnswer.Items.Count - 1;

                lastMessageId = messageId;

                if (File.Exists(filePath))
                    File.Delete(filePath);

                videoMessageFilePath = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ارسال ویدیو:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void DeleteVideoFile()
        {
            if (!string.IsNullOrWhiteSpace(
                videoMessageFilePath))
            {
                try
                {
                    if (File.Exists(
                        videoMessageFilePath))
                    {
                        File.Delete(
                            videoMessageFilePath);
                    }
                }
                catch
                {
                }

                videoMessageFilePath = null;
            }
        }

        private void CloseVideoPreview()
        {
            try
            {
                if (videoPreviewPictureBox != null)
                {
                    Image oldImage =
                        videoPreviewPictureBox.Image;

                    videoPreviewPictureBox.Image =
                        null;

                    if (oldImage != null)
                        oldImage.Dispose();

                    videoPreviewPictureBox.Dispose();
                    videoPreviewPictureBox = null;
                }
            }
            catch
            {
            }

            try
            {
                if (videoPreviewForm != null)
                {
                    videoPreviewForm.FormClosing -=
                        VideoPreviewForm_FormClosing;

                    if (!videoPreviewForm.IsDisposed)
                        videoPreviewForm.Close();

                    videoPreviewForm.Dispose();

                    videoPreviewForm = null;
                }
            }
            catch
            {
            }

            videoTimerLabel = null;
        }

        private void CleanupVideoResources()
        {
            try
            {
                isRecordingVideo = false;

                if (videoMessageTimer != null)
                {
                    videoMessageTimer.Stop();

                    videoMessageTimer.Tick -=
                        VideoMessageTimer_Tick;

                    videoMessageTimer.Dispose();

                    videoMessageTimer = null;
                }
            }
            catch
            {
            }

            try
            {
                if (videoSource != null)
                {
                    videoSource.NewFrame -=
                        VideoSource_NewFrame;

                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        videoSource.WaitForStop();
                    }

                    videoSource.Stop();
                    if (videoSource != null)
                    {
                        if (videoSource.IsRunning)
                        {
                            videoSource.SignalToStop();
                            videoSource.WaitForStop();
                        }

                        videoSource = null;
                    }

                    videoSource = null;
                }
            }
            catch
            {
            }

            try
            {
                if (videoWriter != null)
                {
                    videoWriter.Release();
                    videoWriter.Dispose();

                    videoWriter = null;
                }
            }
            catch
            {
            }

            CloseVideoPreview();

            btnVoicemessage.Text =
                "🎥";

            DeleteVideoFile();
        }
        private void listAnswer_DoubleClick(
      object sender,
      EventArgs e)
        {
            if (listAnswer.SelectedItem == null)
                return;

            if (!(listAnswer.SelectedItem is ChatMessage message))
                return;

            DialogResult result = MessageBox.Show(
                "آیا می‌خواهید این پیام حذف شود؟",
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
                DELETE FROM YourMessages
                WHERE Id = @MessageId
                  AND (
                        YourId = @CurrentUserId
                        OR AnswerId = @CurrentUserId
                      );";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@MessageId",
                            SqlDbType.Int).Value =
                            message.MessageId;

                        command.Parameters.Add(
                            "@CurrentUserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        int deletedRows =
                            command.ExecuteNonQuery();

                        if (deletedRows == 0)
                        {
                            MessageBox.Show(
                                "این پیام قابل حذف نیست.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return;
                        }
                    }
                }

                listAnswer.Items.Remove(message);

                listAnswer.Refresh();
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
        private void VoicePlayer_PlaybackStopped(
            object sender,
            StoppedEventArgs e)
        {
            StopVoice();
        }

        private void StopVoice()
        {
            if (voicePlayer != null)
            {
                voicePlayer.PlaybackStopped -=
                    VoicePlayer_PlaybackStopped;

                voicePlayer.Stop();
                voicePlayer.Dispose();

                voicePlayer = null;
            }

            if (voiceReader != null)
            {
                voiceReader.Dispose();
                voiceReader = null;
            }

            if (!string.IsNullOrEmpty(
                playingVoiceFile))
            {
                try
                {
                    if (File.Exists(
                        playingVoiceFile))
                    {
                        File.Delete(
                            playingVoiceFile);
                    }
                }
                catch
                {
                }

                playingVoiceFile = null;
            }
        }
        private async void btnVideoCall_Click(
            object sender,
            EventArgs e)
        {
            if (currentUserId == 0)
                return;

            if (selectedUserId == 0)
            {
                MessageBox.Show(
                    "ابتدا یک کاربر را انتخاب کنید.");

                return;
            }

            if (!CanMessageSelectedUser())
                return;

            if (callConnection == null ||
                callConnection.State !=
                HubConnectionState.Connected)
            {
                MessageBox.Show(
                    "به سرور تماس متصل نیستید.");

                return;
            }

            try
            {
                await callConnection.InvokeAsync(
                    "CallUser",
                    currentUserId.ToString(),
                    selectedUserId.ToString());

                MessageBox.Show(
                    "درخواست تماس ارسال شد.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ارسال تماس:\n\n" +
                    ex.Message);
            }
        }

        private async Task ConnectToCallServer()
        {
            try
            {
                if (currentUserId == 0)
                    return;

                if (callConnection != null &&
                    callConnection.State ==
                    HubConnectionState.Connected)
                    return;

                callConnection =
                    new HubConnectionBuilder()
                    .WithUrl(
                        "http://localhost:5291/callHub")
                    .WithAutomaticReconnect()
                    .Build();

                callConnection.On<string>(
                    "IncomingCall",
                    async callerId =>
                    {
                        await ShowIncomingCall(
                            callerId);
                    });

                callConnection.On<string>(
                    "CallAccepted",
                    receiverId =>
                    {
                        BeginInvoke(
                            new Action(() =>
                            {
                                if (!int.TryParse(
                                    receiverId,
                                    out int receiverIdInt))
                                    return;

                                VideoCallForm videoCall =
                                    new VideoCallForm(
                                        currentUserId,
                                        receiverIdInt,
                                        true);

                                videoCall.Show(this);
                            }));
                    });

                callConnection.On<string>(
                    "CallRejected",
                    receiverId =>
                    {
                        BeginInvoke(
                            new Action(() =>
                            {
                                MessageBox.Show(
                                    "تماس شما رد شد.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }));
                    });

                callConnection.On<string>(
                    "CallEnded",
                    callerId =>
                    {
                        BeginInvoke(
                            new Action(() =>
                            {
                                MessageBox.Show(
                                    "تماس پایان یافت.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }));
                    });

                await callConnection.StartAsync();

                await callConnection.InvokeAsync(
                    "RegisterUser",
                    currentUserId.ToString());
            }
            catch
            {
            }
        }

        private async Task ShowIncomingCall(
            string callerId)
        {
            if (!int.TryParse(
                callerId,
                out int callerIdInt))
                return;

            if (IsUserBlocked(callerIdInt))
            {
                try
                {
                    await callConnection.InvokeAsync(
                        "RejectCall",
                        callerIdInt.ToString(),
                        currentUserId.ToString());
                }
                catch
                {
                }

                return;
            }

            string callerName =
                await GetUserName(
                    callerIdInt);

            BeginInvoke(
                new Action(async () =>
                {
                    using (IncomingCall incomingCall =
                           new IncomingCall(
                               callerIdInt,
                               currentUserId,
                               callerName))
                    {
                        DialogResult result =
                            incomingCall.ShowDialog(this);

                        if (result ==
                            DialogResult.OK)
                        {
                            if (IsUserBlocked(
                                callerIdInt))
                            {
                                MessageBox.Show(
                                    "این کاربر بلاک شده است.");

                                return;
                            }

                            await callConnection
                                .InvokeAsync(
                                    "AcceptCall",
                                    callerIdInt.ToString(),
                                    currentUserId.ToString());

                            VideoCallForm videoCall =
                                new VideoCallForm(
                                    currentUserId,
                                    callerIdInt,
                                    false);

                            videoCall.Show(this);
                        }
                        else
                        {
                            await callConnection
                                .InvokeAsync(
                                    "RejectCall",
                                    callerIdInt.ToString(),
                                    currentUserId.ToString());
                        }
                    }
                }));
        }

        private async Task<string> GetUserName(
            int userId)
        {
            try
            {
                using (SqlConnection connection =
                       new SqlConnection(
                           connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT FirstAndLastName
                        FROM Users
                        WHERE Id = @Id";

                    using (SqlCommand command =
                           new SqlCommand(
                               query,
                               connection))
                    {
                        command.Parameters.Add(
                            "@Id",
                            SqlDbType.Int).Value =
                            userId;

                        object result =
                            await command
                                .ExecuteScalarAsync();

                        if (result != null &&
                            result != DBNull.Value)
                        {
                            return result.ToString();
                        }

                        return "کاربر ناشناس";
                    }
                }
            }
            catch
            {
                return "کاربر ناشناس";
            }
        }
        private void btnFile_Click(
            object sender,
            EventArgs e)
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            if (!CanMessageSelectedUser())
                return;

            using (OpenFileDialog ofd =
                   new OpenFileDialog())
            {
                ofd.Title =
                    "انتخاب فایل";

                ofd.Filter =
                    "Files|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.txt;*.zip;*.rar;*.7z;*.mp3;*.wav;*.mp4|All Files|*.*";

                if (ofd.ShowDialog() !=
                    DialogResult.OK)
                    return;

                string extension =
                    Path.GetExtension(
                        ofd.FileName)
                    .ToLower();

                string[] imageExtensions =
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".bmp",
                    ".webp"
                };

                if (imageExtensions.Contains(
                    extension))
                {
                    MessageBox.Show(
                        "ارسال عکس از این قسمت امکان‌پذیر نیست.");

                    return;
                }

                try
                {
                    byte[] fileData =
                        File.ReadAllBytes(
                            ofd.FileName);

                    string fileName =
                        Path.GetFileName(
                            ofd.FileName);

                    using (SqlConnection con =
                           new SqlConnection(connectionString))
                    {
                        string query = @"
                            INSERT INTO YourMessages
                            (
                                YourId,
                                YourMessage,
                                AnswerId,
                                AnswerMessage,
                                IsRead,
                                MessageType,
                                FileName,
                                FileData,
                                SentAt
                            )
                            VALUES
                            (
                                @YourId,
                                '',
                                @AnswerId,
                                '',
                                0,
                                'File',
                                @FileName,
                                @FileData,
                                GETDATE()
                            )";

                        using (SqlCommand cmd =
                               new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add(
                                "@YourId",
                                SqlDbType.Int).Value =
                                currentUserId;

                            cmd.Parameters.Add(
                                "@AnswerId",
                                SqlDbType.Int).Value =
                                selectedUserId;

                            cmd.Parameters.Add(
                                "@FileName",
                                SqlDbType.NVarChar,
                                255).Value =
                                fileName;

                            cmd.Parameters.Add(
                                "@FileData",
                                SqlDbType.VarBinary,
                                -1).Value =
                                fileData;

                            con.Open();

                            cmd.ExecuteNonQuery();
                        }
                    }

                    UpdateLastMessageId();

                    MessageBox.Show(
                        "فایل با موفقیت ارسال شد.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در ارسال فایل:\n\n" +
                        ex.Message);
                }
            }
        }
        private void btnSendImage_Click(
            object sender,
            EventArgs e)
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            if (!CanMessageSelectedUser())
                return;

            using (OpenFileDialog ofd =
                   new OpenFileDialog())
            {
                ofd.Title =
                    "انتخاب عکس";

                ofd.Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp";

                if (ofd.ShowDialog() !=
                    DialogResult.OK)
                    return;

                try
                {
                    byte[] imageData =
                        File.ReadAllBytes(
                            ofd.FileName);

                    using (SqlConnection con =
                           new SqlConnection(connectionString))
                    {
                        string query = @"
                            INSERT INTO YourMessages
                            (
                                YourId,
                                YourMessage,
                                AnswerId,
                                AnswerMessage,
                                IsRead,
                                MessageType,
                                ImageData,
                                SentAt
                            )
                            VALUES
                            (
                                @YourId,
                                '',
                                @AnswerId,
                                '',
                                0,
                                'Image',
                                @ImageData,
                                GETDATE()
                            )";

                        using (SqlCommand cmd =
                               new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add(
                                "@YourId",
                                SqlDbType.Int).Value =
                                currentUserId;

                            cmd.Parameters.Add(
                                "@AnswerId",
                                SqlDbType.Int).Value =
                                selectedUserId;

                            cmd.Parameters.Add(
                                "@ImageData",
                                SqlDbType.VarBinary,
                                -1).Value =
                                imageData;

                            con.Open();

                            cmd.ExecuteNonQuery();
                        }
                    }

                    UpdateLastMessageId();

                    MessageBox.Show(
                        "عکس با موفقیت ارسال شد.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در ارسال عکس:\n\n" +
                        ex.Message);
                }
            }
        }
        private void btnLocation_Click(
            object sender,
            EventArgs e)
        {
            if (currentUserId == 0 ||
                selectedUserId == 0)
                return;

            if (!CanMessageSelectedUser())
                return;

            double latitude = 51.5074;
            double longitude = -0.1278;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        INSERT INTO YourMessages
                        (
                            YourId,
                            YourMessage,
                            AnswerId,
                            AnswerMessage,
                            IsRead,
                            MessageType,
                            Latitude,
                            Longitude,
                            SentAt
                        )
                        VALUES
                        (
                            @YourId,
                            '',
                            @AnswerId,
                            '',
                            0,
                            'Location',
                            @Latitude,
                            @Longitude,
                            GETDATE()
                        )";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@YourId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        cmd.Parameters.Add(
                            "@AnswerId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        cmd.Parameters.Add(
                            "@Latitude",
                            SqlDbType.Float).Value =
                            latitude;

                        cmd.Parameters.Add(
                            "@Longitude",
                            SqlDbType.Float).Value =
                            longitude;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }

                UpdateLastMessageId();

                MessageBox.Show(
                    "موقعیت مکانی ارسال شد.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ارسال موقعیت:\n\n" +
                    ex.Message);
            }
        }
        private void btnEmoji_Click(
            object sender,
            EventArgs e)
        {
            ContextMenuStrip emojiMenu =
                new ContextMenuStrip();

            string[] emojis =
            {
                "😀","😃","😄","😁","😆","😅",
                "😂","🤣","😊","😇","🙂","🙃",
                "😉","😌","😍","🥰","😘","😗",
                "😎","🤩","🥳","😏","😢","😭",
                "😡","🤬","😱","😴","🤔","🤗",
                "😐","😑","🙄","😮","😲","🥺",
                "👍","👎","👏","🙌","🙏","👌",
                "✌️","🤝","💪","❤️","🧡","💛",
                "💚","💙","💜","🖤","🤍","💔",
                "🔥","⭐","✨","🎉","🎊","💯",
                "😂","🤣","😜","😋","🤪","😎"
            };

            foreach (string emoji in emojis)
            {
                ToolStripButton button =
                    new ToolStripButton();

                button.Text =
                    emoji;

                button.Font =
                    new Font(
                        "Segoe UI Emoji",
                        16);

                button.AutoSize = true;

                button.Click +=
                    (s, ev) =>
                    {
                        txtYourMessage.AppendText(
                            emoji);

                        emojiMenu.Close();
                    };

                emojiMenu.Items.Add(
                    button);
            }

            emojiMenu.Show(
                btnEmoji,
                new System.Drawing.Point(
                    0,
                    btnEmoji.Height));
        }
        private void txtYourMessage_TextChanged(
            object sender,
            EventArgs e)
        {
            UpdateEmptyMessagePanels();
        }

        private void UpdateEmptyMessagePanels()
        {
            bool hasText =
                !string.IsNullOrWhiteSpace(
                    txtYourMessage.Text);

            pictureBox1.Visible =
                !hasText;

            label4.Visible =
                !hasText;

            bool hasMessages =
                listAnswer.Items.Count > 0;

            pictureBox2.Visible =
                !hasMessages;

            label5.Visible =
                !hasMessages;
        }
        private async void banUserToolStripMenuItem_Click(
            object sender,
            EventArgs e)
        {
            Hide();

            BanUser banUser =
                new BanUser(userPhone);

            banUser.ShowDialog();

            Show();

            await LoadChatHistory();

            UpdateEmptyMessagePanels();
        }

        private void listAnswer_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
        }
        private void listAnswer_DrawItem(
     object sender,
     DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            object item =
                listAnswer.Items[e.Index];

            string text = "";
            string senderName = "";
            string dateTimeText = "";
            bool isMyMessage = false;

            ChatMessage privateMessage =
                item as ChatMessage;

            GroupMessage groupMessage =
                item as GroupMessage;

            if (privateMessage != null)
            {
                text =
                    privateMessage.Text ?? "";

                isMyMessage =
                    privateMessage.SenderId ==
                    currentUserId;

                dateTimeText =
                    privateMessage.SentAt.ToString(
                        "HH:mm  yyyy/MM/dd");
            }

            if (groupMessage != null)
            {
                senderName =
                    groupMessage.SenderName ?? "";

                text =
                    groupMessage.MessageText ?? "";

                isMyMessage =
                    groupMessage.SenderId ==
                    currentUserId;

                if (!isMyMessage &&
                    !string.IsNullOrWhiteSpace(senderName))
                {
                    text =
                        senderName +
                        ": " +
                        text;
                }

                dateTimeText =
                    groupMessage.SentAt.ToString(
                        "HH:mm  yyyy/MM/dd");
            }

            if (privateMessage == null &&
                groupMessage == null)
            {
                return;
            }

            Color messageColor =
                isMyMessage
                ? Color.FromArgb(232, 245, 233)
                : Color.FromArgb(255, 248, 230);

            using (Brush backgroundBrush =
                   new SolidBrush(messageColor))
            {
                e.Graphics.FillRectangle(
                    backgroundBrush,
                    e.Bounds);
            }

            int currentY =
                e.Bounds.Top + 5;

            if (privateMessage != null &&
                privateMessage.ReplyToMessageId.HasValue &&
                !string.IsNullOrWhiteSpace(
                    privateMessage.ReplyToText))
            {
                Rectangle replyRectangle =
                    new Rectangle(
                        e.Bounds.Left + 8,
                        currentY,
                        e.Bounds.Width - 16,
                        28);

                using (Brush replyBackgroundBrush =
                       new SolidBrush(
                           Color.FromArgb(
                               220,
                               230,
                               240)))
                {
                    e.Graphics.FillRectangle(
                        replyBackgroundBrush,
                        replyRectangle);
                }

                string replySender =
                    privateMessage.ReplyToSenderName;

                if (string.IsNullOrWhiteSpace(
                    replySender))
                {
                    replySender = "پیام";
                }

                string replyText =
                    privateMessage.ReplyToText;

                if (replyText.StartsWith("شما: "))
                {
                    replyText =
                        replyText.Substring(5);
                }
                else if (
                    !string.IsNullOrWhiteSpace(
                        label3.Text) &&
                    replyText.StartsWith(
                        label3.Text + ": "))
                {
                    replyText =
                        replyText.Substring(
                            label3.Text.Length + 2);
                }

                if (replyText.Length > 45)
                {
                    replyText =
                        replyText.Substring(
                            0,
                            45) +
                        "...";
                }

                using (Font replySenderFont =
                       new Font(
                           e.Font.FontFamily,
                           8,
                           FontStyle.Bold))
                using (Brush replySenderBrush =
                       new SolidBrush(
                           Color.FromArgb(
                               30,
                               100,
                               180)))
                {
                    e.Graphics.DrawString(
                        "↩ " +
                        replySender,
                        replySenderFont,
                        replySenderBrush,
                        replyRectangle.Left + 8,
                        replyRectangle.Top + 3);
                }

                using (Font replyTextFont =
                       new Font(
                           e.Font.FontFamily,
                           8))
                using (Brush replyTextBrush =
                       new SolidBrush(
                           Color.FromArgb(
                               80,
                               80,
                               80)))
                {
                    e.Graphics.DrawString(
                        replyText,
                        replyTextFont,
                        replyTextBrush,
                        replyRectangle.Left + 8,
                        replyRectangle.Top + 14);
                }

                currentY += 32;
            }

            using (Brush textBrush =
                   new SolidBrush(
                       Color.FromArgb(
                           45,
                           45,
                           45)))
            {
                e.Graphics.DrawString(
                    text,
                    e.Font,
                    textBrush,
                    e.Bounds.Left + 8,
                    currentY);
            }

            using (Font dateFont =
                   new Font(
                       e.Font.FontFamily,
                       8))
            using (Brush dateBrush =
                   new SolidBrush(
                       Color.FromArgb(
                           120,
                           120,
                           120)))
            {
                SizeF textSize =
                    e.Graphics.MeasureString(
                        text,
                        e.Font);

                e.Graphics.DrawString(
                    dateTimeText,
                    dateFont,
                    dateBrush,
                    e.Bounds.Left +
                    12 +
                    textSize.Width +
                    10,
                    currentY + 2);
            }

            if (privateMessage != null &&
                isMyMessage)
            {
                string checks =
                    privateMessage.IsRead
                    ? "✓✓"
                    : "✓";

                Color checkColor =
                    privateMessage.IsRead
                    ? Color.FromArgb(
                        46,
                        125,
                        50)
                    : Color.FromArgb(
                        130,
                        130,
                        130);

                using (Font checkFont =
                       new Font(
                           e.Font.FontFamily,
                           9))
                using (Brush checkBrush =
                       new SolidBrush(checkColor))
                {
                    SizeF textSize =
                        e.Graphics.MeasureString(
                            text,
                            e.Font);

                    SizeF dateSize =
                        e.Graphics.MeasureString(
                            dateTimeText,
                            checkFont);

                    e.Graphics.DrawString(
                        checks,
                        checkFont,
                        checkBrush,
                        e.Bounds.Left +
                        12 +
                        textSize.Width +
                        dateSize.Width +
                        20,
                        currentY);
                }
            }

            e.DrawFocusRectangle();
        }
        private void groupBox2_Enter(
            object sender,
            EventArgs e)
        {
        }

        private void groupBox3_Enter(
            object sender,
            EventArgs e)
        {
        }
        private void btnSound_Click(
            object sender,
            EventArgs e)
        {
            if (recognizer == null)
            {
                MessageBox.Show(
                    "سیستم تشخیص صدا آماده نیست.");

                return;
            }

            if (!isListening)
            {
                try
                {
                    recognizer.RecognizeAsync(
                        RecognizeMode.Multiple);

                    isListening = true;

                    btnSound.Text = "⏹";
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show(
                        "تشخیص صدا در حال اجرا است.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در فعال کردن تایپ صوتی:\n\n" +
                        ex.Message);
                }
            }
            else
            {
                try
                {
                    recognizer.RecognizeAsyncStop();

                    isListening = false;

                    btnSound.Text = "🎙";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در توقف تایپ صوتی:\n\n" +
                        ex.Message);
                }
            }
        }
        private class ChatHistoryItem
        {
            public int UserId { get; set; }

            public string YourID { get; set; }

            public string FirstAndLastName { get; set; }

            public bool IsBlocked { get; set; }

            public override string ToString()
            {
                if (IsBlocked)
                    return YourID +
                           " - Blocked";

                return YourID;
            }
        }
        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            try
            {
                CleanupVideoResources();
            }
            catch
            {
            }

            try
            {
                if (recognizer != null)
                {
                    if (isListening)
                    {
                        recognizer.RecognizeAsyncStop();

                        isListening = false;
                    }

                    recognizer.Dispose();

                    recognizer = null;
                }
            }
            catch
            {
            }

            try
            {
                if (callConnection != null)
                {
                    callConnection.StopAsync()
                        .GetAwaiter()
                        .GetResult();

                    callConnection.DisposeAsync()
                        .GetAwaiter()
                        .GetResult();

                    callConnection = null;
                }
            }
            catch
            {
            }

            StopVoice();

            base.OnFormClosed(e);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedUserId == 0)
                {
                    MessageBox.Show(
                        "ابتدا یک گفتگو را انتخاب کنید.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                DialogResult result = MessageBox.Show(
                    "مطمئنی می‌خواهی تمام پیام‌های این گفتگو را حذف کنی؟",
                    "حذف تمام پیام‌ها",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;

                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                DELETE FROM YourMessages
                WHERE 
                    (YourId = @CurrentUserId AND AnswerId = @SelectedUserId)
                    OR
                    (YourId = @SelectedUserId AND AnswerId = @CurrentUserId);";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@CurrentUserId",
                            currentUserId);

                        command.Parameters.AddWithValue(
                            "@SelectedUserId",
                            selectedUserId);

                        command.ExecuteNonQuery();
                    }
                }
                listAnswer.Items.Clear();
                lastMessageId = 0;

                MessageBox.Show(
                    "تمام پیام‌های این گفتگو حذف شدند.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در حذف پیام‌ها:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void SaveReaction(int messageId, string reaction)
        {
            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();
                    string countQuery = @"
                SELECT COUNT(*)
                FROM MessageReactions
                WHERE MessageId = @MessageId
                  AND UserId = @UserId
                  AND ReactionType = @ReactionType;";

                    int count = 0;

                    using (SqlCommand command =
                           new SqlCommand(countQuery, connection))
                    {
                        command.Parameters.Add("@MessageId", SqlDbType.Int).Value = messageId;
                        command.Parameters.Add("@UserId", SqlDbType.Int).Value = currentUserId;
                        command.Parameters.Add("@ReactionType", SqlDbType.NVarChar, 20).Value = reaction;

                        count = Convert.ToInt32(command.ExecuteScalar());
                    }

                    if (count > 0)
                    {
                        string deleteQuery = @"
                    DELETE FROM MessageReactions
                    WHERE Id =
                    (
                        SELECT TOP 1 Id
                        FROM MessageReactions
                        WHERE MessageId = @MessageId
                          AND UserId = @UserId
                          AND ReactionType = @ReactionType
                        ORDER BY Id DESC
                    );";

                        using (SqlCommand command =
                               new SqlCommand(deleteQuery, connection))
                        {
                            command.Parameters.Add("@MessageId", SqlDbType.Int).Value = messageId;
                            command.Parameters.Add("@UserId", SqlDbType.Int).Value = currentUserId;
                            command.Parameters.Add("@ReactionType", SqlDbType.NVarChar, 20).Value = reaction;

                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertQuery = @"
                    INSERT INTO MessageReactions
                    (
                        MessageId,
                        UserId,
                        ReactionType,
                        CreatedAt
                    )
                    VALUES
                    (
                        @MessageId,
                        @UserId,
                        @ReactionType,
                        GETDATE()
                    );";

                        using (SqlCommand command =
                               new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.Add("@MessageId", SqlDbType.Int).Value = messageId;
                            command.Parameters.Add("@UserId", SqlDbType.Int).Value = currentUserId;
                            command.Parameters.Add("@ReactionType", SqlDbType.NVarChar, 20).Value = reaction;

                            command.ExecuteNonQuery();
                        }
                    }
                }

                LoadConversation();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در تغییر Reaction:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void btnGIF_Click(object sender, EventArgs e)
        {
            try
            {
                if (!CanMessageSelectedUser())
                    return;

                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = "انتخاب GIF";
                    dialog.Filter = "GIF Files (*.gif)|*.gif";
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() != DialogResult.OK)
                        return;

                    string filePath = dialog.FileName;

                    FileInfo fileInfo = new FileInfo(filePath);

                    if (fileInfo.Length > 20 * 1024 * 1024)
                    {
                        MessageBox.Show(
                            "حجم GIF نباید بیشتر از 20 مگابایت باشد.",
                            "Nexa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    byte[] gifData = File.ReadAllBytes(filePath);

                    if (gifData.Length == 0)
                        return;

                    DialogResult result = MessageBox.Show(
                        "مطمئنی می‌خواهی این GIF را ارسال کنی؟",
                        "ارسال GIF",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return;

                    int messageId;

                    using (SqlConnection connection =
                           new SqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = @"
                    INSERT INTO YourMessages
                    (
                        YourId,
                        YourMessage,
                        AnswerId,
                        AnswerMessage,
                        IsRead,
                        SentAt,
                        MessageType,
                        GifData,
                        FileName
                    )
                    VALUES
                    (
                        @YourId,
                        '',
                        @AnswerId,
                        '',
                        0,
                        GETDATE(),
                        'GIF',
                        @GifData,
                        @FileName
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        using (SqlCommand command =
                               new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue(
                                "@YourId",
                                currentUserId);

                            command.Parameters.AddWithValue(
                                "@AnswerId",
                                selectedUserId);

                            command.Parameters.Add(
                                "@GifData",
                                SqlDbType.VarBinary,
                                -1).Value = gifData;

                            command.Parameters.AddWithValue(
                                "@FileName",
                                Path.GetFileName(filePath));

                            messageId =
                                Convert.ToInt32(command.ExecuteScalar());
                        }
                    }

                    ChatMessage message = new ChatMessage
                    {
                        MessageId = messageId,
                        Text = "شما: 🎞️ GIF",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        MessageType = "GIF",
                        GifData = gifData,
                        FileName = Path.GetFileName(filePath)
                    };

                    listAnswer.Items.Add(message);

                    listAnswer.TopIndex =
                        listAnswer.Items.Count - 1;

                    lastMessageId = messageId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ارسال GIF:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void ShowReactionMenu()
        {
            if (listAnswer.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک پیام را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ChatMessage selectedMessage =
                listAnswer.SelectedItem as ChatMessage;

            if (selectedMessage == null)
                return;

            Form reactionForm = new Form();

            reactionForm.Text = "Reaction";
            reactionForm.StartPosition = FormStartPosition.CenterParent;
            reactionForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            reactionForm.Size = new System.Drawing.Size(350, 100);
            reactionForm.MaximizeBox = false;
            reactionForm.MinimizeBox = false;

            FlowLayoutPanel panel = new FlowLayoutPanel();

            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.WrapContents = false;
            panel.Padding = new Padding(10);

            string[] reactions =
            {
        "❤️",
        "😂",
        "👍",
        "😢",
        "😡",
        "😮"
    };

            foreach (string reaction in reactions)
            {
                Button button = new Button();

                button.Text = reaction;
                button.Font = new Font("Segoe UI Emoji", 18);
                button.Size = new System.Drawing.Size(48, 45);
                button.FlatStyle = FlatStyle.Flat;
                button.Cursor = Cursors.Hand;

                button.Click += (s, e) =>
                {
                    SaveReaction(
                        selectedMessage.MessageId,
                        reaction);

                    reactionForm.Close();
                };

                panel.Controls.Add(button);
            }

            reactionForm.Controls.Add(panel);

            reactionForm.ShowDialog(this);
        }
        private void SaveMessage(int messageId)
        {
            try
            {
                if (currentUserId == 0)
                {
                    MessageBox.Show(
                        "شناسه کاربر فعلی پیدا نشد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (messageId <= 0)
                {
                    MessageBox.Show(
                        "شناسه پیام معتبر نیست.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string checkQuery = @"
                SELECT COUNT(*)
                FROM SavedMessages
                WHERE UserId = @UserId
                  AND MessageId = @MessageId;";

                    using (SqlCommand command =
                           new SqlCommand(checkQuery, connection))
                    {
                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        command.Parameters.Add(
                            "@MessageId",
                            SqlDbType.Int).Value =
                            messageId;

                        int count =
                            Convert.ToInt32(
                                command.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show(
                                "این پیام قبلاً ذخیره شده است.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            return;
                        }
                    }

                    string insertQuery = @"
                INSERT INTO SavedMessages
                (
                    UserId,
                    MessageId,
                    SavedAt
                )
                VALUES
                (
                    @UserId,
                    @MessageId,
                    GETDATE()
                );";

                    using (SqlCommand command =
                           new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        command.Parameters.Add(
                            "@MessageId",
                            SqlDbType.Int).Value =
                            messageId;

                        int result =
                            command.ExecuteNonQuery();

                        if (result == 1)
                        {
                            MessageBox.Show(
                                "پیام با موفقیت ذخیره شد. 💾",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "پیام ذخیره نشد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ذخیره پیام:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void CreateMessageContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem saveItem =
                new ToolStripMenuItem("💾 ذخیره پیام");

            saveItem.Click += SaveMessageMenu_Click;

            menu.Items.Add(saveItem);

            listAnswer.ContextMenuStrip = menu;

            listAnswer.MouseDown += listAnswer_MouseDown;
        }
        private void listAnswer_MouseDown(
     object sender,
     MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int index =
                listAnswer.IndexFromPoint(e.Location);

            if (index < 0 ||
                index >= listAnswer.Items.Count)
                return;

            listAnswer.SelectedIndex = index;

            ChatMessage message =
                listAnswer.Items[index]
                as ChatMessage;

            if (message == null)
                return;

            ContextMenuStrip menu =
                new ContextMenuStrip();

            ToolStripMenuItem replyItem =
                new ToolStripMenuItem(
                    "↩ پاسخ");

            replyItem.Click +=
       (s, args) =>
       {
           ShowReplyPanel(message);
       };

            ToolStripMenuItem saveItem =
                new ToolStripMenuItem(
                    "ذخیره پیام");

            saveItem.Click +=
                (s, args) =>
                {
                    SaveSelectedMessage();
                };

            menu.Items.Add(replyItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(saveItem);

            menu.Show(
                listAnswer,
                e.Location);
        }
        private void SaveSelectedMessage()
        {
            if (listAnswer.SelectedItem == null)
                return;

            if (!(listAnswer.SelectedItem is ChatMessage message))
                return;

            if (message.Id <= 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM SavedMessages
                    WHERE MessageId = @MessageId
                    AND UserId = @UserId
                )
                BEGIN
                    INSERT INTO SavedMessages
                    (
                        MessageId,
                        UserId
                    )
                    VALUES
                    (
                        @MessageId,
                        @UserId
                    )
                END";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@MessageId",
                            SqlDbType.Int).Value = message.Id;

                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value = currentUserId;

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "پیام ذخیره شد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ذخیره پیام:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void SaveMessageMenu_Click(object sender, EventArgs e)
        {
            if (listAnswer.SelectedItem == null)
            {
                MessageBox.Show(
                    "هیچ پیامی انتخاب نشده است.",
                    "DEBUG",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ChatMessage selectedMessage =
                listAnswer.SelectedItem as ChatMessage;

            if (selectedMessage == null)
            {
                MessageBox.Show(
                    "SelectedItem از نوع ChatMessage نیست.",
                    "DEBUG",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            MessageBox.Show(
                "MessageId = " +
                selectedMessage.MessageId +
                "\nCurrentUserId = " +
                currentUserId +
                "\nText = " +
                selectedMessage.Text,
                "DEBUG",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            SaveMessage(selectedMessage.MessageId);
        }
        private void ShowUserInfo(int userId)
        {
            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT
                    FirstAndLastName,
                    YourID,
                    Bio
                FROM Users
                WHERE Id = @UserId;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            userId;

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show(
                                    "اطلاعات کاربر پیدا نشد.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }

                            string name =
                                reader["FirstAndLastName"] == DBNull.Value
                                ? ""
                                : reader["FirstAndLastName"].ToString();

                            string yourId =
                                reader["YourID"] == DBNull.Value
                                ? ""
                                : reader["YourID"].ToString();

                            string bio =
                                reader["Bio"] == DBNull.Value
                                ? "بیوگرافی ثبت نشده است."
                                : reader["Bio"].ToString();

                            MessageBox.Show(
                                "👤 نام: " + name +
                                "\n\n" +
                                "🆔 آیدی: " + yourId +
                                "\n\n" +
                                "📝 بیو:\n" + bio,
                                "اطلاعات کاربر",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در دریافت اطلاعات کاربر:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void btnPinmessages_Click(object sender, EventArgs e)
        {
            ShowReactionMenu();
        }

        private void savedMessagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            SavedMessagesForm messagesForm = new SavedMessagesForm(currentUserId);
            messagesForm.ShowDialog();
        }

        private void listHistory_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int index =
                listHistory.IndexFromPoint(e.Location);

            if (index < 0 || index >= listHistory.Items.Count)
                return;

            listHistory.SelectedIndex = index;

            GroupInfo group =
                listHistory.Items[index] as GroupInfo;

            if (group != null)
            {
                try
                {
                    using (SqlConnection connection =
                           new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string creatorQuery = @"
                    SELECT
                        FirstAndLastName,
                        YourID
                    FROM Users
                    WHERE Id = @CreatedBy;";

                        string creatorName = "نامشخص";
                        string creatorYourID = "";

                        using (SqlCommand creatorCommand =
                               new SqlCommand(
                                   creatorQuery,
                                   connection))
                        {
                            creatorCommand.Parameters.Add(
                                "@CreatedBy",
                                SqlDbType.Int).Value =
                                group.CreatedBy;

                            using (SqlDataReader creatorReader =
                                   creatorCommand.ExecuteReader())
                            {
                                if (creatorReader.Read())
                                {
                                    if (creatorReader["FirstAndLastName"] != DBNull.Value)
                                    {
                                        creatorName =
                                            creatorReader["FirstAndLastName"].ToString();
                                    }

                                    if (creatorReader["YourID"] != DBNull.Value)
                                    {
                                        creatorYourID =
                                            creatorReader["YourID"].ToString();
                                    }
                                }
                            }
                        }
                        string membersQuery = @"
                    SELECT
                        U.Id,
                        U.FirstAndLastName,
                        U.YourID
                    FROM GroupMembers GM
                    INNER JOIN Users U
                        ON U.Id = GM.UserId
                    WHERE GM.GroupId = @GroupId
                    ORDER BY U.FirstAndLastName ASC;";

                        List<string> members =
                            new List<string>();

                        using (SqlCommand command =
                               new SqlCommand(
                                   membersQuery,
                                   connection))
                        {
                            command.Parameters.Add(
                                "@GroupId",
                                SqlDbType.Int).Value =
                                group.Id;

                            using (SqlDataReader reader =
                                   command.ExecuteReader())
                            {
                                int counter = 1;

                                while (reader.Read())
                                {
                                    string name =
                                        reader["FirstAndLastName"] == DBNull.Value
                                        ? "بدون نام"
                                        : reader["FirstAndLastName"].ToString();

                                    string yourId =
                                        reader["YourID"] == DBNull.Value
                                        ? ""
                                        : reader["YourID"].ToString();

                                    bool isOwner =
                                        Convert.ToInt32(reader["Id"]) ==
                                        group.CreatedBy;

                                    string memberText =
                                        counter +
                                        ". " +
                                        name;

                                    if (!string.IsNullOrWhiteSpace(yourId))
                                    {
                                        memberText +=
                                            "  (@" +
                                            yourId +
                                            ")";
                                    }

                                    if (isOwner)
                                    {
                                        memberText +=
                                            "  (Owner)";
                                    }

                                    members.Add(memberText);

                                    counter++;
                                }
                            }
                        }
                        string groupName =
                            string.IsNullOrWhiteSpace(group.GroupName)
                            ? "بدون نام"
                            : group.GroupName;

                        string groupBio =
                            string.IsNullOrWhiteSpace(group.GroupBio)
                            ? "توضیحی ثبت نشده است."
                            : group.GroupBio;

                        string creatorText =
                            creatorName;

                        if (!string.IsNullOrWhiteSpace(creatorYourID))
                        {
                            creatorText +=
                                "  (@" +
                                creatorYourID +
                                ")";
                        }

                        creatorText +=
                            "  (Owner)";

                        string photoText =
                            group.GroupPhoto != null &&
                            group.GroupPhoto.Length > 0
                            ? "دارد"
                            : "ندارد";

                        string membersText;

                        if (members.Count == 0)
                        {
                            membersText =
                                "هیچ عضوی پیدا نشد.";
                        }
                        else
                        {
                            membersText =
                                string.Join(
                                    Environment.NewLine,
                                    members);
                        }
                        string info =
                            "مشخصات گروه" +
                            Environment.NewLine +
                            Environment.NewLine +

                            "نام گروه: " +
                            groupName +
                            Environment.NewLine +
                            Environment.NewLine +

                            "توضیحات: " +
                            groupBio +
                            Environment.NewLine +
                            Environment.NewLine +

                            "شناسه گروه: " +
                            group.Id +
                            Environment.NewLine +
                            Environment.NewLine +

                            "مالک: " +
                            creatorText +
                            Environment.NewLine +
                            Environment.NewLine +

                            "عکس گروه: " +
                            photoText +
                            Environment.NewLine +
                            Environment.NewLine +

                            "تعداد اعضا: " +
                            members.Count +
                            Environment.NewLine +
                            Environment.NewLine +

                            "اعضای گروه:" +
                            Environment.NewLine +
                            membersText;

                        MessageBox.Show(
                            info,
                            "مشخصات گروه",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return;
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(
                        "خطای SQL هنگام دریافت اطلاعات گروه:\n\n" +
                        ex.Message +
                        "\n\nشماره خطا: " +
                        ex.Number,
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در دریافت اطلاعات گروه:\n\n" +
                        ex.Message,
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }
            }
            string yourIdText =
                listHistory.Items[index].ToString();

            if (string.IsNullOrWhiteSpace(yourIdText))
                return;

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT Id
                FROM Users
                WHERE YourID = @YourID;";

                    using (SqlCommand command =
                           new SqlCommand(
                               query,
                               connection))
                    {
                        command.Parameters.Add(
                            "@YourID",
                            SqlDbType.NVarChar,
                            50).Value =
                            yourIdText;

                        object result =
                            command.ExecuteScalar();

                        if (result == null)
                        {
                            MessageBox.Show(
                                "کاربر پیدا نشد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return;
                        }

                        int userId =
                            Convert.ToInt32(result);

                        ShowUserInfo(userId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در دریافت اطلاعات:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void loginHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentUser.Id <= 0)
            {
                MessageBox.Show("کاربر فعلی شناسایی نشد.", "Nexa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoginHistoryForm history =
                new LoginHistoryForm(CurrentUser.Id);

            history.ShowDialog();
        }

        private void creatorToGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
            CreateGroup create = new CreateGroup();
            create.ShowDialog();
        }

        private void listHistory_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            try
            {
                if (currentUserId == 0)
                    return;

                if (selectedUserId == 0)
                    return;

                LoadNewMessages();
                RefreshReadStatuses();
            }
            catch
            {
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedGroupId <= 0)
            {
                MessageBox.Show(
                    "ابتدا یک گروه را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT CreatedBy
                FROM Groups
                WHERE Id = @GroupId;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            selectedGroupId;

                        object result =
                            command.ExecuteScalar();

                        if (result == null)
                        {
                            MessageBox.Show(
                                "گروه پیدا نشد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return;
                        }

                        int ownerId =
                            Convert.ToInt32(result);

                        if (ownerId != currentUserId)
                        {
                            MessageBox.Show(
                                "فقط مالک گروه می‌تواند عضو جدید اضافه کند.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return;
                        }
                    }
                }

                using (AddGroupMember form =
                       new AddGroupMember(selectedGroupId))
                {
                    DialogResult result =
                        form.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        LoadGroupMembersAfterAdd();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در افزودن عضو:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void LoadGroupMembersAfterAdd()
        {
            if (selectedGroupId <= 0)
                return;

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT
                    U.FirstAndLastName,
                    U.YourID
                FROM GroupMembers GM
                INNER JOIN Users U
                    ON U.Id = GM.UserId
                WHERE GM.GroupId = @GroupId
                ORDER BY U.FirstAndLastName ASC;";

                    List<string> members =
                        new List<string>();

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            selectedGroupId;

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            int counter = 1;

                            while (reader.Read())
                            {
                                string name =
                                    reader["FirstAndLastName"] == DBNull.Value
                                    ? "بدون نام"
                                    : reader["FirstAndLastName"].ToString();

                                string yourId =
                                    reader["YourID"] == DBNull.Value
                                    ? ""
                                    : reader["YourID"].ToString();

                                string member =
                                    counter +
                                    ". " +
                                    name;

                                if (!string.IsNullOrWhiteSpace(yourId))
                                {
                                    member +=
                                        "  (@" +
                                        yourId +
                                        ")";
                                }

                                members.Add(member);

                                counter++;
                            }
                        }
                    }

                    string text =
                        "تعداد اعضا: " +
                        members.Count +
                        Environment.NewLine +
                        Environment.NewLine +
                        string.Join(
                            Environment.NewLine,
                            members);

                    MessageBox.Show(
                        text,
                        "اعضای گروه",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بروزرسانی اعضای گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void inviteLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedGroupId <= 0)
            {
                MessageBox.Show(
                    "ابتدا یک گروه را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT
                    G.GroupName,
                    G.InviteCode,
                    G.CreatedBy
                FROM Groups G
                WHERE G.Id = @GroupId;";

                    string groupName = "";
                    string inviteCode = "";
                    int createdBy = 0;

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@GroupId",
                            SqlDbType.Int).Value =
                            selectedGroupId;

                        using (SqlDataReader reader =
                               command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show(
                                    "گروه پیدا نشد.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }

                            groupName =
                                reader["GroupName"] == DBNull.Value
                                ? ""
                                : reader["GroupName"].ToString();

                            inviteCode =
                                reader["InviteCode"] == DBNull.Value
                                ? ""
                                : reader["InviteCode"].ToString();

                            createdBy =
                                reader["CreatedBy"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    reader["CreatedBy"]);
                        }
                    }

                    if (createdBy != currentUserId)
                    {
                        MessageBox.Show(
                            "فقط مالک گروه می‌تواند لینک دعوت ایجاد کند.",
                            "Nexa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    if (string.IsNullOrWhiteSpace(inviteCode))
                    {
                        inviteCode =
                            Guid.NewGuid()
                            .ToString("N")
                            .Substring(0, 12)
                            .ToUpper();

                        string updateQuery = @"
                    UPDATE Groups
                    SET InviteCode = @InviteCode
                    WHERE Id = @GroupId;";

                        using (SqlCommand updateCommand =
                               new SqlCommand(
                                   updateQuery,
                                   connection))
                        {
                            updateCommand.Parameters.Add(
                                "@InviteCode",
                                SqlDbType.NVarChar,
                                100).Value =
                                inviteCode;

                            updateCommand.Parameters.Add(
                                "@GroupId",
                                SqlDbType.Int).Value =
                                selectedGroupId;

                            updateCommand.ExecuteNonQuery();
                        }
                    }

                    string inviteLink =
                        "https://nexa.app/group/" +
                        inviteCode;

                    Clipboard.SetText(inviteLink);

                    MessageBox.Show(
                        "گروه: " +
                        groupName +
                        "\n\n" +
                        "Invite Link:\n" +
                        inviteLink +
                        "\n\n" +
                        "لینک کپی شد.",
                        "Group Invite Link",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "خطای SQL:\n\n" +
                    ex.Message +
                    "\n\nشماره خطا: " +
                    ex.Number,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ایجاد لینک دعوت:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void joinGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string link = "";

            using (Form inputForm = new Form())
            {
                inputForm.Text = "Join Group";
                inputForm.Width = 450;
                inputForm.Height = 180;
                inputForm.StartPosition = FormStartPosition.CenterParent;

                Label label = new Label();
                label.Text = "لینک دعوت گروه را وارد کنید:";
                label.Left = 20;
                label.Top = 20;
                label.Width = 380;

                TextBox textBox = new TextBox();
                textBox.Left = 20;
                textBox.Top = 50;
                textBox.Width = 380;

                Button btnOk = new Button();
                btnOk.Text = "OK";
                btnOk.Left = 220;
                btnOk.Top = 90;
                btnOk.Width = 80;
                btnOk.DialogResult = DialogResult.OK;

                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Left = 310;
                btnCancel.Top = 90;
                btnCancel.Width = 90;
                btnCancel.DialogResult = DialogResult.Cancel;

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(btnOk);
                inputForm.Controls.Add(btnCancel);

                inputForm.AcceptButton = btnOk;
                inputForm.CancelButton = btnCancel;

                if (inputForm.ShowDialog(this) != DialogResult.OK)
                    return;

                link = textBox.Text.Trim();
            }

            if (string.IsNullOrWhiteSpace(link))
                return;

            int lastSlash =
                link.LastIndexOf("/");

            if (lastSlash < 0 ||
                lastSlash >= link.Length - 1)
            {
                MessageBox.Show(
                    "لینک دعوت نامعتبر است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string inviteCode =
                link.Substring(lastSlash + 1).Trim();

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                MessageBox.Show(
                    "کد دعوت نامعتبر است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (currentUserId <= 0)
            {
                MessageBox.Show(
                    "کاربر فعلی شناسایی نشده است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                using (JoinGroup form =
                       new JoinGroup(
                           inviteCode,
                           currentUserId))
                {
                    DialogResult result =
                        form.ShowDialog(this);

                    if (result == DialogResult.OK)
                    {
                        await LoadChatHistory();

                        listHistory.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در ورود به گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void UpdateMyLastSeen()
        {
            if (currentUserId <= 0)
                return;

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                UPDATE Users
                SET LastSeen = GETDATE()
                WHERE Id = @UserId;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        connection.Open();

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }
        private void CheckSelectedUserStatus()
        {
            if (selectedUserId <= 0)
            {
                label6.Text = "";
                return;
            }

            try
            {
                using (SqlConnection connection =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT LastSeen
                FROM Users
                WHERE Id = @UserId;";

                    using (SqlCommand command =
                           new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            selectedUserId;

                        connection.Open();

                        object result =
                            command.ExecuteScalar();

                        if (result == null ||
                            result == DBNull.Value)
                        {
                            label6.Text = "Offline";
                            return;
                        }

                        DateTime lastSeen =
                            Convert.ToDateTime(result);

                        TimeSpan difference =
                            DateTime.Now - lastSeen;

                        if (difference.TotalSeconds <= 10)
                        {
                            label6.Text = "Online";
                        }
                        else
                        {
                            label6.Text = "Offline";
                        }
                    }
                }
            }
            catch
            {
                label6.Text = "Offline";
            }
        }
        private async Task TestApiConnection()
        {
            try
            {
                HttpResponseMessage response =
                    await apiClient.GetAsync("api/users/1");

                string result =
                    await response.Content.ReadAsStringAsync();

                MessageBox.Show(
                    "Status: " +
                    response.StatusCode +
                    "\n\n" +
                    result,
                    "Nexa API",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در اتصال به API:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private async Task<UserResponse> GetUserFromApi(int userId)
        {
            try
            {
                HttpResponseMessage response =
                    await apiClient.GetAsync(
                        "api/users/" + userId);

                string result =
                    await response.Content.ReadAsStringAsync();

                MessageBox.Show(
                    "StatusCode: " +
                    response.StatusCode +
                    "\n\nResponse:\n" +
                    result,
                    "API Debug",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (!response.IsSuccessStatusCode)
                    return null;

                UserResponse user =
                    await response.Content
                        .ReadFromJsonAsync<UserResponse>();

                return user;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "API Error:\n\n" +
                    ex.ToString(),
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }
        }
        private async Task TestGetUserFromApi()
        {
            if (currentUserId <= 0)
                return;

            UserResponse user =
                await GetUserFromApi(currentUserId);

            if (user == null)
            {
                MessageBox.Show(
                    "کاربر از API دریافت نشد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            MessageBox.Show(
                "Id: " +
                user.Id +
                "\nName: " +
                user.FirstAndLastName +
                "\nYourID: " +
                user.YourID,
                "API Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        private async Task<List<ApiChatItem>> GetChatHistoryFromApi(int userId)
        {
            try
            {
                HttpResponseMessage response =
                    await apiClient.GetAsync(
                        "api/chat/history/" + userId);

                string json =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        "خطا از API:\n\n" +
                        json,
                        "Nexa API",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return new List<ApiChatItem>();
                }

                List<ApiChatItem> items =
                    System.Text.Json.JsonSerializer.Deserialize<List<ApiChatItem>>(
                        json);

                if (items == null)
                    return new List<ApiChatItem>();

                return items;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در دریافت تاریخچه از API:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return new List<ApiChatItem>();
            }
        }
        private async Task LoadChatHistoryFromDatabase()
        {
            listHistory.Items.Clear();

            if (currentUserId <= 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    // =====================================================
                    // 1. کاربران
                    // =====================================================

                    string userQuery = @"
                SELECT DISTINCT
                    U.Id,
                    U.YourID,
                    U.FirstAndLastName
                FROM ChatHistory CH
                INNER JOIN Users U
                    ON
                    (
                        CH.UserId = @UserId
                        AND U.Id = CH.OtherUserId
                    )
                    OR
                    (
                        CH.OtherUserId = @UserId
                        AND U.Id = CH.UserId
                    )
                WHERE
                    CH.UserId = @UserId
                    OR
                    CH.OtherUserId = @UserId
                ORDER BY U.FirstAndLastName";

                    using (SqlCommand cmd =
                           new SqlCommand(userQuery, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        using (SqlDataReader reader =
                               await cmd.ExecuteReaderAsync())
                        {
                            HashSet<int> addedUsers =
                                new HashSet<int>();

                            while (await reader.ReadAsync())
                            {
                                int userId =
                                    Convert.ToInt32(reader["Id"]);

                                if (addedUsers.Contains(userId))
                                    continue;

                                addedUsers.Add(userId);

                                ChatHistoryItem item =
                                    new ChatHistoryItem();

                                item.UserId =
                                    userId;

                                item.YourID =
                                    reader["YourID"]?.ToString()
                                    ?? "";

                                item.FirstAndLastName =
                                    reader["FirstAndLastName"]?.ToString()
                                    ?? "";

                                item.IsBlocked =
                                    IsUserBlocked(userId);

                                listHistory.Items.Add(item);
                            }
                        }
                    }

                    // =====================================================
                    // 2. گروه‌ها
                    // =====================================================

                    string groupQuery = @"
                SELECT
                    G.Id,
                    G.GroupName,
                    G.GroupBio,
                    G.GroupPhoto,
                    G.CreatedBy,
                    G.InviteCode
                FROM GroupMembers GM
                INNER JOIN Groups G
                    ON G.Id = GM.GroupId
                WHERE GM.UserId = @UserId
                ORDER BY G.GroupName";

                    using (SqlCommand cmd =
                           new SqlCommand(groupQuery, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        using (SqlDataReader reader =
                               await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                GroupInfo group =
                                    new GroupInfo();

                                group.Id =
                                    Convert.ToInt32(
                                        reader["Id"]);

                                group.GroupName =
                                    reader["GroupName"]?.ToString()
                                    ?? "";

                                group.GroupBio =
                                    reader["GroupBio"]?.ToString()
                                    ?? "";

                                group.CreatedBy =
                                    Convert.ToInt32(
                                        reader["CreatedBy"]);

                                group.InviteCode =
                                    reader["InviteCode"]?.ToString()
                                    ?? "";

                                // ============================
                                // عکس گروه
                                // ============================

                                if (reader["GroupPhoto"] != DBNull.Value)
                                {
                                    try
                                    {
                                        if (reader["GroupPhoto"]
                                            is byte[])
                                        {
                                            group.GroupPhoto =
                                                (byte[])reader["GroupPhoto"];
                                        }
                                        else
                                        {
                                            string photo =
                                                reader["GroupPhoto"]
                                                .ToString();

                                            if (!string.IsNullOrWhiteSpace(photo))
                                            {
                                                group.GroupPhoto =
                                                    Convert.FromBase64String(
                                                        photo);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        group.GroupPhoto = null;
                                    }
                                }
                                else
                                {
                                    group.GroupPhoto = null;
                                }

                                listHistory.Items.Add(group);
                            }
                        }
                    }
                }

                listHistory.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در بارگذاری تاریخچه کاربران و گروه‌ها:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private async Task ConfirmDeleteGroup(GroupInfo group)
{
    if (group == null)
        return;

    if (group.CreatedBy != currentUserId)
    {
        MessageBox.Show(
            "فقط مالک گروه می‌تواند گروه را حذف کند.",
            "Nexa",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        return;
    }

    DialogResult result =
        MessageBox.Show(
            "آیا مطمئن هستید که می‌خواهید گروه \"" +
            group.GroupName +
            "\" را حذف کنید؟\n\n" +
            "تمام اعضا و پیام‌های این گروه نیز حذف خواهند شد.",
            "حذف گروه",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

    if (result != DialogResult.Yes)
        return;

    await DeleteGroup(group.Id);
}
        private async Task DeleteGroup(int groupId)
        {
            if (groupId <= 0)
                return;

            if (currentUserId <= 0)
                return;

            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    using (SqlTransaction transaction =
                           con.BeginTransaction())
                    {
                        try
                        {
                            string ownerQuery = @"
                        SELECT CreatedBy
                        FROM Groups
                        WHERE Id = @GroupId";

                            int createdBy = 0;

                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       ownerQuery,
                                       con,
                                       transaction))
                            {
                                cmd.Parameters.Add(
                                    "@GroupId",
                                    SqlDbType.Int).Value =
                                    groupId;

                                object result =
                                    await cmd.ExecuteScalarAsync();

                                if (result == null ||
                                    result == DBNull.Value)
                                {
                                    transaction.Rollback();

                                    MessageBox.Show(
                                        "گروه پیدا نشد.",
                                        "Nexa",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);

                                    return;
                                }

                                createdBy =
                                    Convert.ToInt32(result);
                            }

                            if (createdBy != currentUserId)
                            {
                                transaction.Rollback();

                                MessageBox.Show(
                                    "فقط مالک گروه می‌تواند گروه را حذف کند.",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }

                            string deleteMessages = @"
                        DELETE FROM GroupMessages
                        WHERE GroupId = @GroupId";

                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       deleteMessages,
                                       con,
                                       transaction))
                            {
                                cmd.Parameters.Add(
                                    "@GroupId",
                                    SqlDbType.Int).Value =
                                    groupId;

                                await cmd.ExecuteNonQueryAsync();
                            }

                            string deleteMembers = @"
                        DELETE FROM GroupMembers
                        WHERE GroupId = @GroupId";

                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       deleteMembers,
                                       con,
                                       transaction))
                            {
                                cmd.Parameters.Add(
                                    "@GroupId",
                                    SqlDbType.Int).Value =
                                    groupId;

                                await cmd.ExecuteNonQueryAsync();
                            }

                            string deleteGroup = @"
                        DELETE FROM Groups
                        WHERE Id = @GroupId
                        AND CreatedBy = @UserId";

                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       deleteGroup,
                                       con,
                                       transaction))
                            {
                                cmd.Parameters.Add(
                                    "@GroupId",
                                    SqlDbType.Int).Value =
                                    groupId;

                                cmd.Parameters.Add(
                                    "@UserId",
                                    SqlDbType.Int).Value =
                                    currentUserId;

                                int affected =
                                    await cmd.ExecuteNonQueryAsync();

                                if (affected == 0)
                                {
                                    throw new Exception(
                                        "گروه حذف نشد.");
                                }
                            }

                            transaction.Commit();

                            await LoadChatHistoryFromDatabase();

                            listHistory.Refresh();

                            MessageBox.Show(
                                "گروه با موفقیت حذف شد.",
                                "Nexa",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        catch
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch
                            {
                            }

                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در حذف گروه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void btnEditMessage_Click(object sender, EventArgs e)
        {
            if (listAnswer.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک پیام را از لیست انتخاب کنید.");

                return;
            }

            await EditSelectedMessage();
        }
        private async Task EditSelectedMessage()
        {
            if (listAnswer.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک پیام را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!(listAnswer.SelectedItem is ChatMessage message))
            {
                MessageBox.Show(
                    "پیام انتخاب شده معتبر نیست.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (message.MessageId <= 0)
            {
                MessageBox.Show(
                    "شناسه پیام معتبر نیست.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (message.MessageType != "Text")
            {
                MessageBox.Show(
                    "فقط پیام‌های متنی قابل ویرایش هستند.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            string currentText =
                message.Text ?? "";

            if (currentText.StartsWith("شما: "))
            {
                currentText =
                    currentText.Substring(5);
            }

            if (string.IsNullOrWhiteSpace(currentText))
            {
                MessageBox.Show(
                    "متن پیام خالی است.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            using (EditMessageForm editForm =
                   new EditMessageForm(currentText))
            {
                DialogResult result =
                    editForm.ShowDialog(this);

                if (result != DialogResult.OK)
                    return;

                string newText =
                    editForm.EditedMessage;

                if (string.IsNullOrWhiteSpace(newText))
                    return;

                if (newText == currentText)
                    return;

                try
                {
                    using (SqlConnection con =
                           new SqlConnection(connectionString))
                    {
                        await con.OpenAsync();

                        string query = @"
                    UPDATE YourMessages
                    SET YourMessage = @NewMessage
                    WHERE Id = @MessageId
                    AND YourId = @UserId";

                        using (SqlCommand cmd =
                               new SqlCommand(query, con))
                        {
                            cmd.Parameters.Add(
                                "@NewMessage",
                                SqlDbType.NVarChar,
                                -1).Value =
                                newText;

                            cmd.Parameters.Add(
                                "@MessageId",
                                SqlDbType.Int).Value =
                                message.MessageId;

                            cmd.Parameters.Add(
                                "@UserId",
                                SqlDbType.Int).Value =
                                currentUserId;

                            int affected =
                                await cmd.ExecuteNonQueryAsync();

                            if (affected == 0)
                            {
                                MessageBox.Show(
                                    "فقط فرستنده میتواند پیام را ویرایش کند",
                                    "Nexa",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }
                        }
                    }

                    message.Text =
                        "شما: " + newText;

                    listAnswer.Refresh();

                    MessageBox.Show(
                        "پیام با موفقیت ویرایش شد.",
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "خطا در ویرایش پیام:\n\n" +
                        ex.Message,
                        "Nexa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        private void CreateReplyPanel()
        {
            replyPanel = new Panel();

            replyPanel.Height = 70;
            replyPanel.Dock = DockStyle.Top;
            replyPanel.Visible = false;
            replyPanel.BackColor = Color.FromArgb(35, 35, 45);

            replySenderLabel = new Label();

            replySenderLabel.AutoSize = false;
            replySenderLabel.SetBounds(
                15,
                8,
                400,
                22);

            replySenderLabel.Font =
                new Font(
                    "Segoe UI",
                    9,
                    FontStyle.Bold);

            replySenderLabel.ForeColor =
                Color.DeepSkyBlue;

            replyTextLabel = new Label();

            replyTextLabel.AutoSize = false;
            replyTextLabel.SetBounds(
                15,
                30,
                400,
                30);

            replyTextLabel.Font =
                new Font(
                    "Segoe UI",
                    9);

            replyTextLabel.ForeColor =
                Color.White;

            replyCancelButton = new Button();

            replyCancelButton.Text = "✕";
            replyCancelButton.SetBounds(
                420,
                15,
                40,
                35);

            replyCancelButton.FlatStyle =
                FlatStyle.Flat;

            replyCancelButton.FlatAppearance.BorderSize =
                0;

            replyCancelButton.ForeColor =
                Color.White;

            replyCancelButton.BackColor =
                Color.Transparent;

            replyCancelButton.Click +=
                btnReplay_Click;

            replyPanel.Controls.Add(
                replySenderLabel);

            replyPanel.Controls.Add(
                replyTextLabel);

            replyPanel.Controls.Add(
                replyCancelButton);

            Controls.Add(replyPanel);

            replyPanel.BringToFront();
        }

        private void btnReplay_Click(
     object sender,
     EventArgs e)
        {
            if (replyToMessage != null)
            {
                replyToMessage = null;

                if (replyPanel != null)
                {
                    replyPanel.Visible = false;

                    replySenderLabel.Text = "";

                    replyTextLabel.Text = "";
                }

                txtYourMessage.Focus();

                return;
            }

            if (listAnswer.SelectedItem == null)
            {
                MessageBox.Show(
                    "ابتدا یک پیام را انتخاب کنید.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!(listAnswer.SelectedItem
                  is ChatMessage message))
            {
                MessageBox.Show(
                    "پیام انتخاب شده معتبر نیست.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ShowReplyPanel(message);
        }
        private void ShowReplyPanel(
    ChatMessage message)
        {
            if (message == null)
                return;

            replyToMessage = message;

            string senderName;

            if (message.SenderId == currentUserId)
            {
                senderName = "شما";
            }
            else
            {
                senderName = label3.Text;
            }

            string text =
                message.Text ?? "";

            if (text.StartsWith("شما: "))
            {
                text =
                    text.Substring(5);
            }
            else if (
                !string.IsNullOrWhiteSpace(label3.Text) &&
                text.StartsWith(label3.Text + ": "))
            {
                text =
                    text.Substring(
                        label3.Text.Length + 2);
            }

            if (text.Length > 70)
            {
                text =
                    text.Substring(0, 70) +
                    "...";
            }

            replySenderLabel.Text =
                "↩ پاسخ به " + senderName;

            replyTextLabel.Text =
                text;

            replyPanel.Visible = true;

            replyPanel.BringToFront();

            txtYourMessage.Focus();
        }
    }
}