namespace Nexa
{
    partial class BanUser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BanUser));
            this.btnBack = new System.Windows.Forms.Button();
            this.listBan = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnBan = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.btnDeleteBan = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnBack
            // 
            this.btnBack.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnBack.Location = new System.Drawing.Point(0, 462);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(387, 30);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // listBan
            // 
            this.listBan.FormattingEnabled = true;
            this.listBan.ItemHeight = 16;
            this.listBan.Location = new System.Drawing.Point(12, 201);
            this.listBan.Name = "listBan";
            this.listBan.Size = new System.Drawing.Size(362, 180);
            this.listBan.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(99, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "UserId";
            // 
            // btnBan
            // 
            this.btnBan.Location = new System.Drawing.Point(27, 387);
            this.btnBan.Name = "btnBan";
            this.btnBan.Size = new System.Drawing.Size(136, 37);
            this.btnBan.TabIndex = 4;
            this.btnBan.Text = "Ban";
            this.btnBan.UseVisualStyleBackColor = true;
            this.btnBan.Click += new System.EventHandler(this.btnBan_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(92, 61);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(184, 22);
            this.txtSearch.TabIndex = 5;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged_1);
            // 
            // lstResults
            // 
            this.lstResults.FormattingEnabled = true;
            this.lstResults.ItemHeight = 16;
            this.lstResults.Location = new System.Drawing.Point(12, 95);
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(362, 100);
            this.lstResults.TabIndex = 6;
            // 
            // btnDeleteBan
            // 
            this.btnDeleteBan.Location = new System.Drawing.Point(234, 387);
            this.btnDeleteBan.Name = "btnDeleteBan";
            this.btnDeleteBan.Size = new System.Drawing.Size(136, 37);
            this.btnDeleteBan.TabIndex = 7;
            this.btnDeleteBan.Text = "DeleteBan";
            this.btnDeleteBan.UseVisualStyleBackColor = true;
            this.btnDeleteBan.Click += new System.EventHandler(this.btnDeleteBan_Click);
            // 
            // BanUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(387, 492);
            this.Controls.Add(this.btnDeleteBan);
            this.Controls.Add(this.lstResults);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnBan);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBan);
            this.Controls.Add(this.btnBack);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BanUser";
            this.Text = "BanUser";
            this.Load += new System.EventHandler(this.BanUser_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.ListBox listBan;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnBan;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.Button btnDeleteBan;
    }
}