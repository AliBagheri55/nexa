namespace Nexa
{
    partial class JoinGroup
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
            this.picGroupPhoto = new System.Windows.Forms.PictureBox();
            this.lblGroupName = new System.Windows.Forms.Label();
            this.lblGroupBio = new System.Windows.Forms.Label();
            this.lblMemberCount = new System.Windows.Forms.Label();
            this.btnJoin = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picGroupPhoto)).BeginInit();
            this.SuspendLayout();
            // 
            // picGroupPhoto
            // 
            this.picGroupPhoto.Location = new System.Drawing.Point(144, 12);
            this.picGroupPhoto.Name = "picGroupPhoto";
            this.picGroupPhoto.Size = new System.Drawing.Size(160, 121);
            this.picGroupPhoto.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picGroupPhoto.TabIndex = 0;
            this.picGroupPhoto.TabStop = false;
            // 
            // lblGroupName
            // 
            this.lblGroupName.AutoSize = true;
            this.lblGroupName.Location = new System.Drawing.Point(203, 170);
            this.lblGroupName.Name = "lblGroupName";
            this.lblGroupName.Size = new System.Drawing.Size(44, 16);
            this.lblGroupName.TabIndex = 1;
            this.lblGroupName.Text = "label1";
            // 
            // lblGroupBio
            // 
            this.lblGroupBio.AutoSize = true;
            this.lblGroupBio.Location = new System.Drawing.Point(203, 218);
            this.lblGroupBio.Name = "lblGroupBio";
            this.lblGroupBio.Size = new System.Drawing.Size(44, 16);
            this.lblGroupBio.TabIndex = 2;
            this.lblGroupBio.Text = "label1";
            // 
            // lblMemberCount
            // 
            this.lblMemberCount.AutoSize = true;
            this.lblMemberCount.Location = new System.Drawing.Point(203, 275);
            this.lblMemberCount.Name = "lblMemberCount";
            this.lblMemberCount.Size = new System.Drawing.Size(44, 16);
            this.lblMemberCount.TabIndex = 3;
            this.lblMemberCount.Text = "label1";
            // 
            // btnJoin
            // 
            this.btnJoin.Location = new System.Drawing.Point(36, 330);
            this.btnJoin.Name = "btnJoin";
            this.btnJoin.Size = new System.Drawing.Size(136, 54);
            this.btnJoin.TabIndex = 4;
            this.btnJoin.Text = "Join";
            this.btnJoin.UseVisualStyleBackColor = true;
            this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(270, 330);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(136, 54);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // JoinGroup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 415);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnJoin);
            this.Controls.Add(this.lblMemberCount);
            this.Controls.Add(this.lblGroupBio);
            this.Controls.Add(this.lblGroupName);
            this.Controls.Add(this.picGroupPhoto);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "JoinGroup";
            this.Text = "JoinGroup";
            this.Load += new System.EventHandler(this.JoinGroup_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picGroupPhoto)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picGroupPhoto;
        private System.Windows.Forms.Label lblGroupName;
        private System.Windows.Forms.Label lblGroupBio;
        private System.Windows.Forms.Label lblMemberCount;
        private System.Windows.Forms.Button btnJoin;
        private System.Windows.Forms.Button btnCancel;
    }
}