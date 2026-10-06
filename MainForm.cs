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
        private Cp2112RawHid cp;

        public MainForm()
        {
            Text = "استوديو خدمة بطاريات اللابتوب";
            Width = 1180; Height = 760;
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
            Font = new Font("Segoe UI", 10);

            Panel header = new Panel { Dock=DockStyle.Top, Height=76 };
            Label title = new Label { Text="استوديو خدمة بطاريات اللابتوب", Dock=DockStyle.Right, Width=470,
                Font=new Font("Segoe UI",20,FontStyle.Bold), TextAlign=ContentAlignment.MiddleCenter };
            status.Text="الحالة: غير متصل"; status.Dock=DockStyle.Left; status.Width=300; status.TextAlign=ContentAlignment.MiddleCenter;
            header.Controls.Add(title); header.Controls.Add(status);

            FlowLayoutPanel side=new FlowLayoutPanel { Dock=DockStyle.Right, Width=230, FlowDirection=FlowDirection.TopDown, Padding=new Padding(12), WrapContents=false };
            connect.Text="اتصال CP2112"; connect.Width=200; connect.Height=44; connect.Click+=delegate{Connect();};
            read.Text="قراءة بيانات البطارية"; read.Width=200; read.Height=44; read.Enabled=false; read.Click+=delegate{ReadBattery();};
            Button info=new Button {Text="معلومات البرنامج",Width=200,Height=44};
            info.Click+=delegate{MessageBox.Show("Battery Service Studio\r\nبديل مستقل لخدمة بطاريات اللابتوب\r\nواجهة عربية - Windows 7 / Windows 10 / Windows 11\r\n\r\nالاتصال: CP2112 HID/SMBus","حول البرنامج",MessageBoxButtons.OK,MessageBoxIcon.Information);};
            side.Controls.Add(connect); side.Controls.Add(read); side.Controls.Add(info);

            data.Dock=DockStyle.Fill; data.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; data.AllowUserToAddRows=false; data.RowHeadersVisible=false;
            data.Columns.Add("name","البيان"); data.Columns.Add("value","القيمة");
            Add("الجهد","—"); Add("التيار","—"); Add("درجة الحرارة","—"); Add("السعة المتبقية","—"); Add("السعة الكاملة","—");
            Add("عدد الدورات","—"); Add("حالة البطارية","—"); Add("الرقم التسلسلي","—"); Add("الشركة","—"); Add("اسم البطارية","—");

            log.Multiline=true; log.ReadOnly=true; log.Dock=DockStyle.Bottom; log.Height=150; log.ScrollBars=ScrollBars.Vertical;
            log.RightToLeft=RightToLeft.Yes;
            Controls.Add(data); Controls.Add(side); Controls.Add(log); Controls.Add(header);
            FormClosing+=delegate{if(cp!=null)cp.Dispose();};
            Log("جاهز. صِل محول CP2112 ثم اضغط «اتصال CP2112».");
        }

        private void Add(string n,string v){data.Rows.Add(n,v);}
        private void SetValue(int row,string value){data.Rows[row].Cells[1].Value=value;}

        private void Connect()
        {
            try
            {
                if(cp!=null)cp.Dispose();
                cp=new Cp2112RawHid();
                if(!cp.Open())
                {
                    status.Text="الحالة: لم يتم العثور على CP2112"; status.ForeColor=Color.DarkRed;
                    Log("لم يتم العثور على جهاز CP2112 (VID 10C4 / PID EA90).");
                    MessageBox.Show("لم يتم العثور على محول CP2112.\r\n\r\nتأكد من توصيل USB وظهوره في «إدارة الأجهزة» ضمن Human Interface Devices.","CP2112",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }
                status.Text="الحالة: CP2112 متصل"; status.ForeColor=Color.DarkGreen; read.Enabled=true;
                Log("تم العثور على CP2112 وفتح قناة HID/SMBus بنجاح.");
            }
            catch(Exception ex)
            {
                status.Text="الحالة: خطأ في CP2112"; status.ForeColor=Color.DarkRed;
                Log("خطأ: "+ex.Message);
                MessageBox.Show(ex.Message,"خطأ CP2112",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void ReadBattery()
        {
            if(cp==null || !cp.IsConnected){MessageBox.Show("اتصل بـ CP2112 أولاً.");return;}
            read.Enabled=false; status.Text="الحالة: قراءة البطارية...";
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    ushort voltage=cp.ReadWord(0x09);
                    short current=cp.ReadSignedWord(0x0A);
                    ushort temp=cp.ReadWord(0x08);
                    ushort rem=cp.ReadWord(0x0F);
                    ushort full=cp.ReadWord(0x10);
                    ushort cycles=cp.ReadWord(0x17);
                    ushort bst=cp.ReadWord(0x16);
                    ushort serial=cp.ReadWord(0x1C);
                    string manufacturer=cp.ReadText(0x20);
                    string name=cp.ReadText(0x21);

                    BeginInvoke((MethodInvoker)delegate
                    {
                        SetValue(0,voltage+" mV");
                        SetValue(1,current+" mA");
                        SetValue(2,(temp/10.0-273.15).ToString("0.0")+" °C");
                        SetValue(3,rem+" mAh"); SetValue(4,full+" mAh"); SetValue(5,cycles.ToString());
                        SetValue(6,"0x"+bst.ToString("X4")); SetValue(7,serial.ToString());
                        SetValue(8,string.IsNullOrEmpty(manufacturer)?"—":manufacturer);
                        SetValue(9,string.IsNullOrEmpty(name)?"—":name);
                        status.Text="الحالة: تمّت القراءة"; status.ForeColor=Color.DarkGreen;
                        Log("تمت قراءة بيانات Smart Battery القياسية عبر SMBus.");
                        read.Enabled=true;
                    });
                }
                catch(Exception ex)
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        status.Text="الحالة: فشل القراءة"; status.ForeColor=Color.DarkRed;
                        Log("فشل قراءة البطارية: "+ex.Message);
                        MessageBox.Show("تعذر قراءة البطارية عبر SMBus.\r\n\r\n"+ex.Message+"\r\n\r\nإذا كان CP2112 متصلاً، تأكد من توصيل SDA/SCL والأرضي مع البطارية بشكل صحيح.","قراءة البطارية",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        read.Enabled=true;
                    });
                }
            });
        }

        private void Log(string s){log.AppendText("["+DateTime.Now.ToString("HH:mm:ss")+"] "+s+Environment.NewLine);}
    }
}