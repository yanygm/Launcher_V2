using Launcher.Properties;

namespace KartRider
{
    partial class GetKart
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
            button1 = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            ItemType = new System.Windows.Forms.ComboBox();
            ItemID = new System.Windows.Forms.ComboBox();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(189, 15);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(64, 50);
            button1.TabIndex = 0;
            button1.Text = "添加";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(10, 16);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(35, 12);
            label1.TabIndex = 1;
            label1.Text = "类型:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(10, 55);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(35, 12);
            label2.TabIndex = 2;
            label2.Text = "道具:";
            // 
            // ItemType
            // 
            ItemType.FormattingEnabled = true;
            ItemType.Location = new System.Drawing.Point(46, 12);
            ItemType.Name = "ItemType";
            ItemType.Size = new System.Drawing.Size(121, 20);
            ItemType.TabIndex = 3;
            ItemType.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // ItemID
            // 
            ItemID.FormattingEnabled = true;
            ItemID.Location = new System.Drawing.Point(46, 51);
            ItemID.Name = "ItemID";
            ItemID.Size = new System.Drawing.Size(121, 20);
            ItemID.TabIndex = 4;
            ItemID.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            // 
            // GetKart
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(273, 100);
            Controls.Add(ItemID);
            Controls.Add(ItemType);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button1);
            Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GetKart";
            Icon = Resources.icon;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "添加道具";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox ItemType;
        private System.Windows.Forms.ComboBox ItemID;
    }
}
