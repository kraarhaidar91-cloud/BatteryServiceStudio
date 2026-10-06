using System;
using System.Drawing;
using System.Windows.Forms;

namespace BatteryServiceStudio
{
    public sealed class MainForm : Form
    {
        private readonly Label status = new Label();
        private readonly TextBox log = new TextBox();
        private readonly DataGridView data = new DataGridView();
        private readonly Button connect = new Button();
        private readonly Button read = new Button();

        public MainForm()
        {
            Text = "استوديو خدمة بطاريات اللابتوب";
            Width = 1180; Height = 760;
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            Font = new Font("Segoe UI", 10);

            Panel header = new Panel { Dock = DockStyle.Top, Height = 76 };
            Label title = new Label {
                Text = "استوديو خدمة بطاريات اللابتوب", Dock = DockStyle.Right, Width = 470,
                Font = new Font("Segoe UI", 20, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
            };
            status.Text = "الحالة: غير متصل";
            status.Dock = DockStyle.Left; status.Width = 260; status.TextAlign = ContentAlignment.MiddleCenter;
            header.Controls.Add(title); header.Controls.Add(status);

            FlowLayoutPanel side = new FlowLayoutPanel {
                Dock = DockStyle.Right, Width = 230, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(12), WrapContents = false
            };
            connect.Text = "اتصال CP2112"; connect.Width = 200; connect.Height = 44;
            connect.Click += delegate { Connect(); };
            read.Text = "قراءة بيانات البطارية"; read.Width = 200; read.Height = 44; read.Enabled = false;
            read.Click += delegate { ReadBattery(); };
            Button info = new Button { Text = "معلومات البرنامج", Width = 200, Height = 44 };
            info.Click += delegate {
                MessageBox.Show(
                    "Battery Service Studio\r\nبديل مستقل لخدمة بطاريات اللابتوب\r\nواجهة عربية - Windows 7 / Windows 10 / Windows 11",
                    "حول البرنامج", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            side.Controls.Add(connect); side.Controls.Add(read); side.Controls.Add(info);

            data.Dock = DockStyle.Fill;
            data.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            data.AllowUserToAddRows = false; data.RowHeadersVisible = false;
            data.Columns.Add("name", "البيان"); data.Columns.Add("value", "القيمة");
            Add("الجهد", "—"); Add("التيار", "—"); Add("درجة الحرارة", "—");
            Add("السعة المتبقية", "—"); Add("السعة الكاملة", "—"); Add("عدد الدورات", "—");
            Add("حالة البطارية", "—"); Add("الرقم التسلسلي", "—"); Add("الشركة", "—"); Add("اسم البطارية", "—");

            log.Multiline = true; log.ReadOnly = true; log.Dock = DockStyle.Bottom;
            log.Height = 150; log.ScrollBars = ScrollBars.Vertical; log.RightToLeft = RightToLeft.Yes;

            Controls.Add(data); Controls.Add(side); Controls.Add(log); Controls.Add(header);
            Log("جاهز. صِل محول CP2112 ثم اضغط «اتصال CP2112».");
        }

        private void Add(string name, string value) { data.Rows.Add(name, value); }

        private void Connect()
        {
            status.Text = "الحالة: جاهز لواجهة CP2112";
            status.ForeColor = Color.DarkGreen;
            read.Enabled = true;
            Log("تم تهيئة واجهة البرنامج. طبقة SMBus ستُربط مع CP2112 في مرحلة الاتصال الفعلي.");
        }

        private void ReadBattery()
        {
            Log("بدء قراءة Smart Battery القياسية...");
            MessageBox.Show(
                "هذه النسخة متوافقة مع Windows 7/10/11، وتم تجهيز واجهة CP2112.\r\n\r\nعمليات القراءة والكتابة الفعلية عبر SMBus ستُضاف بعد اختبار المحول والبطارية.",
                "Battery Service Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Log(string message)
        {
            log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        }
    }
}