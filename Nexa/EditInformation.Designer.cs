namespace Nexa
{
    partial class EditInformation
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
            this.button1 = new System.Windows.Forms.Button();
            this.btnOkEdit = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtPhone = new System.Windows.Forms.TextBox();
            this.txtId = new System.Windows.Forms.TextBox();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.btnEditProfile = new System.Windows.Forms.Button();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblId = new System.Windows.Forms.Label();
            this.lblPhon = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.btnPassShow = new System.Windows.Forms.Button();
            this.pickphoto = new System.Windows.Forms.PictureBox();
            this.txtBio = new System.Windows.Forms.TextBox();
            this.lblBio = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pickphoto)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.button1.Location = new System.Drawing.Point(0, 585);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(406, 36);
            this.button1.TabIndex = 0;
            this.button1.Text = "Back";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnOkEdit
            // 
            this.btnOkEdit.Location = new System.Drawing.Point(134, 526);
            this.btnOkEdit.Name = "btnOkEdit";
            this.btnOkEdit.Size = new System.Drawing.Size(137, 39);
            this.btnOkEdit.TabIndex = 1;
            this.btnOkEdit.Text = "Edit";
            this.btnOkEdit.UseVisualStyleBackColor = true;
            this.btnOkEdit.Click += new System.EventHandler(this.btnOkEdit_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(28, 457);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(279, 22);
            this.txtPassword.TabIndex = 2;
            // 
            // txtPhone
            // 
            this.txtPhone.Location = new System.Drawing.Point(28, 391);
            this.txtPhone.Name = "txtPhone";
            this.txtPhone.Size = new System.Drawing.Size(351, 22);
            this.txtPhone.TabIndex = 3;
            // 
            // txtId
            // 
            this.txtId.Location = new System.Drawing.Point(28, 281);
            this.txtId.Name = "txtId";
            this.txtId.Size = new System.Drawing.Size(351, 22);
            this.txtId.TabIndex = 4;
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(28, 215);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(351, 22);
            this.txtUsername.TabIndex = 5;
            // 
            // btnEditProfile
            // 
            this.btnEditProfile.Location = new System.Drawing.Point(137, 117);
            this.btnEditProfile.Name = "btnEditProfile";
            this.btnEditProfile.Size = new System.Drawing.Size(111, 34);
            this.btnEditProfile.TabIndex = 7;
            this.btnEditProfile.Text = "Edit Profile";
            this.btnEditProfile.UseVisualStyleBackColor = true;
            this.btnEditProfile.Click += new System.EventHandler(this.btnEditProfile_Click);
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(25, 196);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(100, 16);
            this.lblUsername.TabIndex = 8;
            this.lblUsername.Text = "New Username";
            // 
            // lblId
            // 
            this.lblId.AutoSize = true;
            this.lblId.Location = new System.Drawing.Point(25, 262);
            this.lblId.Name = "lblId";
            this.lblId.Size = new System.Drawing.Size(48, 16);
            this.lblId.TabIndex = 9;
            this.lblId.Text = "New Id";
            // 
            // lblPhon
            // 
            this.lblPhon.AutoSize = true;
            this.lblPhon.Location = new System.Drawing.Point(25, 372);
            this.lblPhon.Name = "lblPhon";
            this.lblPhon.Size = new System.Drawing.Size(46, 16);
            this.lblPhon.TabIndex = 10;
            this.lblPhon.Text = "Phone";
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(25, 438);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(67, 16);
            this.lblPassword.TabIndex = 11;
            this.lblPassword.Text = "Password";
            // 
            // btnPassShow
            // 
            this.btnPassShow.Image = global::Nexa.Properties.Resources.eye_show_icon_191607;
            this.btnPassShow.Location = new System.Drawing.Point(313, 457);
            this.btnPassShow.Name = "btnPassShow";
            this.btnPassShow.Size = new System.Drawing.Size(66, 23);
            this.btnPassShow.TabIndex = 12;
            this.btnPassShow.UseVisualStyleBackColor = true;
            this.btnPassShow.Click += new System.EventHandler(this.btnPassShow_Click);
            // 
            // pickphoto
            // 
            this.pickphoto.Location = new System.Drawing.Point(112, 12);
            this.pickphoto.Name = "pickphoto";
            this.pickphoto.Size = new System.Drawing.Size(161, 99);
            this.pickphoto.TabIndex = 6;
            this.pickphoto.TabStop = false;
            // 
            // txtBio
            // 
            this.txtBio.Location = new System.Drawing.Point(28, 337);
            this.txtBio.Name = "txtBio";
            this.txtBio.Size = new System.Drawing.Size(351, 22);
            this.txtBio.TabIndex = 13;
            // 
            // lblBio
            // 
            this.lblBio.AutoSize = true;
            this.lblBio.Location = new System.Drawing.Point(29, 318);
            this.lblBio.Name = "lblBio";
            this.lblBio.Size = new System.Drawing.Size(27, 16);
            this.lblBio.TabIndex = 14;
            this.lblBio.Text = "Bio";
            // 
            // EditInformation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 621);
            this.Controls.Add(this.lblBio);
            this.Controls.Add(this.txtBio);
            this.Controls.Add(this.btnPassShow);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.lblPhon);
            this.Controls.Add(this.lblId);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.btnEditProfile);
            this.Controls.Add(this.pickphoto);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.txtId);
            this.Controls.Add(this.txtPhone);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.btnOkEdit);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "EditInformation";
            this.Text = "EditInformation";
            this.Load += new System.EventHandler(this.EditInformation_Load_1);
            ((System.ComponentModel.ISupportInitialize)(this.pickphoto)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnOkEdit;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtPhone;
        private System.Windows.Forms.TextBox txtId;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.PictureBox pickphoto;
        private System.Windows.Forms.Button btnEditProfile;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.Label lblPhon;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Button btnPassShow;
        private System.Windows.Forms.TextBox txtBio;
        private System.Windows.Forms.Label lblBio;
    }
}