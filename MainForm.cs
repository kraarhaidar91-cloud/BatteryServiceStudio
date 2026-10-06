using System.Drawing;
using System.Windows.Forms;

namespace BatteryServiceStudio;

public sealed class MainForm : Form
{
    private readonly Label status = new();
    private readonly TextBox log = new();
    private readonly DataGridView data = new();
    private readonly Button connect = new();
    private readonly Button read = new();

    public MainForm()
    {
        Text = "Battery Service Studio";
        Width = 1180; Height = 760;
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10);

        var header = new Panel { Dock=DockStyle.Top, Height=76 };
        var title = new Label { Text="استوديو خدمة بطاريات اللابتوب", Dock=DockStyle.Right, Width=470, Font=new Font("Segoe UI",20,FontStyle.Bold), TextAlign=ContentAlignment.MiddleCenter };
        status.Text="الحالة: غير متصل"; status.Dock=DockStyle.Left; status.Width=260; status.TextAlign=ContentAlignment.MiddleCenter;
        header.Controls.Add(title); header.Controls.Add(status);

        var side = new FlowLayoutPanel { Dock=DockStyle.Right, Width=230, FlowDirection=FlowDirection.TopDown, Padding=new Padding(12), WrapContents=false };
        connect.Text="اتصال CP2112"; connect.Width=200; connect.Height=44; connect.Click += (_,_) => Connect();
        read.Text="قراءة بيانات البطارية"; read.Width=200; read.Height=44; read.Enabled=false; read.Click += (_,_) => ReadBattery();
        var info = new Button { Text="معلومات البرنامج", Width=200, Height=44 };
        info.Click += (_,_) => MessageBox.Show("Battery Service Studio\nبديل مستقل لخدمة بطاريات اللابتوب\nواجهة عربية - Windows 10/11", "حول البرنامج");
        side.Controls.Add(connect); side.Controls.Add(read); side.Controls.Add(info);

        data.Dock=DockStyle.Fill; data.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        data.AllowUserToAddRows=false; data.RowHeadersVisible=false;
        data.Columns.Add("name","البيان"); data.Columns.Add("value","القيمة");
        Add("الجهد","—"); Add("التيار","—"); Add("درجة الحرارة","—"); Add("السعة المتبقية","—"); Add("السعة الكاملة","—");
        Add("عدد الدورات","—"); Add("حالة البطارية","—"); Add("الرقم التسلسلي","—"); Add("الشركة","—"); Add("اسم البطارية","—");

        log.Multiline=true; log.ReadOnly=true; log.Dock=DockStyle.Bottom; log.Height=150; log.ScrollBars=ScrollBars.Vertical;
        log.RightToLeft=RightToLeft.Yes;

        Controls.Add(data); Controls.Add(side); Controls.Add(log); Controls.Add(header);
        Log("جاهز. صِل محول CP2112 ثم اضغط «اتصال CP2112».");
    }

    void Add(string n,string v){ data.Rows.Add(n,v); }

    void Connect()
    {
        status.Text="الحالة: جاهز لواجهة CP2112";
        status.ForeColor=Color.DarkGreen;
        read.Enabled=true;
        Log("تم تهيئة جلسة الجهاز. طبقة SMBus جاهزة للإضافة/التعامل مع CP2112.");
    }

    void ReadBattery()
    {
        Log("بدء قراءة Smart Battery القياسية...");
        MessageBox.Show("تم فتح واجهة القراءة.\nفي هذه النسخة يتم تجهيز طبقة الاتصال القياسية قبل تنفيذ عمليات الكتابة الخاصة بكل متحكم.", "Battery Service Studio");
    }

    void Log(string s){ log.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}{Environment.NewLine}"); }
}