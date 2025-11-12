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
            button_Add = new Button();
            label_Type = new Label();
            label_Item = new Label();
            ItemType = new ComboBox();
            ItemID = new ComboBox();
            SuspendLayout();
            // 
            // button_Add
            // 
            button_Add.Location = new Point(189, 12);
            button_Add.Name = "button_Add";
            button_Add.Size = new Size(72, 59);
            button_Add.TabIndex = 0;
            button_Add.Text = "添加";
            button_Add.UseVisualStyleBackColor = true;
            button_Add.Click += button_Add_Click;
            // 
            // label_Type
            // 
            label_Type.AutoSize = true;
            label_Type.Location = new Point(10, 16);
            label_Type.Name = "label_Type";
            label_Type.Size = new Size(35, 12);
            label_Type.TabIndex = 362;
            label_Type.Text = "类型:";
            // 
            // label_Item
            // 
            label_Item.AutoSize = true;
            label_Item.Location = new Point(10, 55);
            label_Item.Name = "label_Item";
            label_Item.Size = new Size(35, 12);
            label_Item.TabIndex = 363;
            label_Item.Text = "道具:";
            // 
            // ItemType
            // 
            ItemType.FormattingEnabled = true;
            ItemType.Location = new Point(46, 12);
            ItemType.Name = "ItemType";
            ItemType.Size = new Size(137, 20);
            ItemType.TabIndex = 364;
            ItemType.SelectedIndexChanged += ItemType_SelectedIndexChanged;
            ItemType.MouseEnter += ItemType_MouseEnter;
            // 
            // ItemID
            // 
            ItemID.FormattingEnabled = true;
            ItemID.Location = new Point(46, 51);
            ItemID.Name = "ItemID";
            ItemID.Size = new Size(137, 20);
            ItemID.TabIndex = 365;
            ItemID.SelectedIndexChanged += ItemID_SelectedIndexChanged;
            ItemID.MouseEnter += ItemID_MouseEnter;
            // 
            // GetKart
            // 
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(273, 100);
            Controls.Add(ItemID);
            Controls.Add(ItemType);
            Controls.Add(label_Item);
            Controls.Add(label_Type);
            Controls.Add(button_Add);
            Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GetKart";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "添加道具";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button button_Add;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label label_Item;
        private System.Windows.Forms.ComboBox ItemType;
        private System.Windows.Forms.ComboBox ItemID;
    }
}
