using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class LoginHistoryForm : Form
    {
        private string connectionString =
            @"Server=.;Database=Nexa;Trusted_Connection=True;TrustServerCertificate=True;";

        private int currentUserId;

        public LoginHistoryForm(int userId)
        {
            InitializeComponent();

            currentUserId = userId;
        }

        private void LoginHistoryForm_Load(object sender, EventArgs e)
        {
            lblTitle.Text = "🔐 Login History";

            ConfigureGrid();

            LoadLoginHistory();

            LoadStatistics();
        }
        private void ConfigureGrid()
        {
            dgvLoginHistory.AutoGenerateColumns = false;
            dgvLoginHistory.AllowUserToAddRows = false;
            dgvLoginHistory.AllowUserToDeleteRows = false;
            dgvLoginHistory.ReadOnly = true;
            dgvLoginHistory.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvLoginHistory.MultiSelect = false;

            dgvLoginHistory.Columns.Clear();

            DataGridViewTextBoxColumn statusColumn =
                new DataGridViewTextBoxColumn();

            statusColumn.Name = "Status";
            statusColumn.HeaderText = "وضعیت";
            statusColumn.DataPropertyName =
                "LoginStatus";
            statusColumn.Width = 100;

            dgvLoginHistory.Columns.Add(statusColumn);

            DataGridViewTextBoxColumn dateColumn =
                new DataGridViewTextBoxColumn();

            dateColumn.Name = "LoginDate";
            dateColumn.HeaderText = "تاریخ و ساعت";
            dateColumn.DataPropertyName =
                "LoginDate";
            dateColumn.Width = 160;

            dgvLoginHistory.Columns.Add(dateColumn);

            DataGridViewTextBoxColumn deviceColumn =
                new DataGridViewTextBoxColumn();

            deviceColumn.Name = "DeviceName";
            deviceColumn.HeaderText = "دستگاه";
            deviceColumn.DataPropertyName =
                "DeviceName";
            deviceColumn.Width = 180;

            dgvLoginHistory.Columns.Add(deviceColumn);

            DataGridViewTextBoxColumn osColumn =
                new DataGridViewTextBoxColumn();

            osColumn.Name = "OperatingSystem";
            osColumn.HeaderText = "سیستم عامل";
            osColumn.DataPropertyName =
                "OperatingSystem";
            osColumn.Width = 220;

            dgvLoginHistory.Columns.Add(osColumn);

            DataGridViewTextBoxColumn ipColumn =
                new DataGridViewTextBoxColumn();

            ipColumn.Name = "IPAddress";
            ipColumn.HeaderText = "IP Address";
            ipColumn.DataPropertyName =
                "IPAddress";
            ipColumn.Width = 130;

            dgvLoginHistory.Columns.Add(ipColumn);

            DataGridViewTextBoxColumn typeColumn =
                new DataGridViewTextBoxColumn();

            typeColumn.Name = "LoginType";
            typeColumn.HeaderText = "نوع ورود";
            typeColumn.DataPropertyName =
                "LoginType";
            typeColumn.Width = 120;

            dgvLoginHistory.Columns.Add(typeColumn);

            dgvLoginHistory.EnableHeadersVisualStyles = false;

            dgvLoginHistory.ColumnHeadersDefaultCellStyle.Font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold);

            dgvLoginHistory.DefaultCellStyle.Font =
                new Font(
                    "Segoe UI",
                    9);

            dgvLoginHistory.RowTemplate.Height = 35;
        }

        private void LoadLoginHistory()
        {
            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            LoginStatus,
                            LoginDate,
                            DeviceName,
                            OperatingSystem,
                            IPAddress,
                            LoginType
                        FROM LoginHistory
                        WHERE UserId = @UserId
                        ORDER BY LoginDate DESC";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        using (SqlDataAdapter adapter =
                               new SqlDataAdapter(cmd))
                        {
                            DataTable table =
                                new DataTable();

                            adapter.Fill(table);

                            dgvLoginHistory.DataSource =
                                table;
                        }
                    }
                }

                FormatStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در دریافت تاریخچه ورود:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void FormatStatus()
        {
            foreach (DataGridViewRow row
                     in dgvLoginHistory.Rows)
            {
                if (row.Cells["Status"].Value == null)
                    continue;

                string status =
                    row.Cells["Status"].Value.ToString();

                if (status == "Success")
                {
                    row.Cells["Status"].Value =
                        "✓ موفق";
                }
                else if (status == "Failed")
                {
                    row.Cells["Status"].Value =
                        "✕ ناموفق";
                }
            }
        }

        private void LoadStatistics()
        {
            try
            {
                using (SqlConnection con =
                       new SqlConnection(connectionString))
                {
                    con.Open();

                    string lastLoginQuery = @"
                        SELECT TOP 1 LoginDate
                        FROM LoginHistory
                        WHERE UserId = @UserId
                        AND LoginStatus = 'Success'
                        ORDER BY LoginDate DESC";

                    using (SqlCommand cmd =
                           new SqlCommand(
                               lastLoginQuery,
                               con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        object result =
                            cmd.ExecuteScalar();

                        if (result != null &&
                            result != DBNull.Value)
                        {
                            DateTime date =
                                Convert.ToDateTime(result);

                            lblLastLogin.Text =
                                "آخرین ورود: " +
                                date.ToString(
                                    "yyyy/MM/dd HH:mm");
                        }
                        else
                        {
                            lblLastLogin.Text =
                                "آخرین ورود: هنوز وارد نشده";
                        }
                    }

                    string failedQuery = @"
                        SELECT COUNT(*)
                        FROM LoginHistory
                        WHERE UserId = @UserId
                        AND LoginStatus = 'Failed'";

                    using (SqlCommand cmd =
                           new SqlCommand(
                               failedQuery,
                               con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        int count =
                            Convert.ToInt32(
                                cmd.ExecuteScalar());

                        lblFailedCount.Text =
                            "تلاش‌های ناموفق: " +
                            count.ToString();
                    }
                }
            }
            catch
            {
                lblLastLogin.Text =
                    "آخرین ورود: نامشخص";

                lblFailedCount.Text =
                    "تلاش‌های ناموفق: نامشخص";
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadLoginHistory();

            LoadStatistics();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            DialogResult result =
              MessageBox.Show(
                  "آیا مطمئن هستید که می‌خواهید تمام تاریخچه ورود خود را حذف کنید؟",
                  "حذف تاریخچه",
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
                        DELETE FROM LoginHistory
                        WHERE UserId = @UserId";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@UserId",
                            SqlDbType.Int).Value =
                            currentUserId;

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }

                LoadLoginHistory();

                LoadStatistics();

                MessageBox.Show(
                    "تاریخچه ورود با موفقیت پاک شد.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در حذف تاریخچه:\n\n" +
                    ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void dgvLoginHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgvLoginHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (dgvLoginHistory.Columns[e.ColumnIndex].Name
                != "Status")
                return;

            if (e.Value == null)
                return;

            string value =
                e.Value.ToString();

            if (value.Contains("موفق"))
            {
                e.CellStyle.Font =
                    new Font(
                        dgvLoginHistory.Font,
                        FontStyle.Bold);
            }
            else if (value.Contains("ناموفق"))
            {
                e.CellStyle.Font =
                    new Font(
                        dgvLoginHistory.Font,
                        FontStyle.Bold);
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
