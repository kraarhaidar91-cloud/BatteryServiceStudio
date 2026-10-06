using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BatteryServiceStudio
{
    public sealed class Cp2112RawHid : IDisposable
    {
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private static readonly IntPtr INVALID = new IntPtr(-1);
        private IntPtr handle = INVALID;
        private string path = "";

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }

        [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("hid.dll", SetLastError=true)] private static extern bool HidD_SetFeature(IntPtr h, byte[] b, int n);
        [DllImport("setupapi.dll", SetLastError=true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr w, uint f);
        [DllImport("setupapi.dll", SetLastError=true)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, uint i, ref SP_DEVICE_INTERFACE_DATA x);
        [DllImport("setupapi.dll", SetLastError=true)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_DEVICE_INTERFACE_DATA x, IntPtr p, uint n, out uint r, IntPtr d);
        [DllImport("setupapi.dll", SetLastError=true)] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)] private static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr sec, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError=true)] private static extern bool ReadFile(IntPtr h, byte[] b, uint n, out uint got, IntPtr ov);
        [DllImport("kernel32.dll", SetLastError=true)] private static extern bool WriteFile(IntPtr h, byte[] b, uint n, out uint written, IntPtr ov);
        [DllImport("kernel32.dll", SetLastError=true)] private static extern bool CloseHandle(IntPtr h);

        public bool IsConnected { get { return handle != INVALID; } }

        public bool Open()
        {
            Close();
            Guid hid; HidD_GetHidGuid(out hid);
            IntPtr set = SetupDiGetClassDevs(ref hid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (set == INVALID) return false;
            try
            {
                uint i = 0;
                while (true)
                {
                    SP_DEVICE_INTERFACE_DATA d = new SP_DEVICE_INTERFACE_DATA();
                    d.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref hid, i++, ref d))
                    {
                        if (Marshal.GetLastWin32Error() == 259) break;
                        continue;
                    }
                    uint required;
                    SetupDiGetDeviceInterfaceDetail(set, ref d, IntPtr.Zero, 0, out required, IntPtr.Zero);
                    if (required == 0) continue;
                    IntPtr mem = Marshal.AllocHGlobal((int)required);
                    try
                    {
                        Marshal.WriteInt32(mem, 5);
                        if (!SetupDiGetDeviceInterfaceDetail(set, ref d, mem, required, out required, IntPtr.Zero)) continue;
                        string p = Marshal.PtrToStringUni(new IntPtr(mem.ToInt64() + 4));
                        if (p == null) continue;
                        string low = p.ToLowerInvariant();
                        if (low.IndexOf("vid_10c4") < 0 || low.IndexOf("pid_ea90") < 0) continue;
                        IntPtr h = CreateFile(p, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                        if (h != INVALID) { handle = h; path = p; Configure(); return true; }
                    }
                    finally { Marshal.FreeHGlobal(mem); }
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
            return false;
        }

        private void Configure()
        {
            byte[] r = new byte[64];
            r[0]=0x06; r[1]=0x00; r[2]=0x01; r[3]=0x86; r[4]=0xA0;
            r[5]=0x02; r[6]=0x01; r[7]=0x03; r[8]=0xE8; r[9]=0x03; r[10]=0xE8;
            r[11]=0x00; r[12]=0x00; r[13]=0x05;
            if (!HidD_SetFeature(handle, r, r.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "تعذر ضبط إعدادات CP2112.");
        }

        public byte[] WriteRead(byte slave7, byte command, int length)
        {
            if (!IsConnected) throw new InvalidOperationException("CP2112 غير متصل.");
            byte[] req = new byte[64];
            req[0]=0x11; req[1]=(byte)(slave7 << 1);
            req[2]=(byte)(length >> 8); req[3]=(byte)length; req[4]=1; req[5]=command;
            uint written;
            if (!WriteFile(handle, req, 64, out written, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "فشل إرسال طلب SMBus.");
            DateTime end=DateTime.Now.AddSeconds(3);
            while(DateTime.Now<end)
            {
                byte[] resp=new byte[64]; uint got;
                if(!ReadFile(handle,resp,64,out got,IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "فشل استقبال CP2112.");
                if(got<3 || resp[0]!=0x13) continue;
                if(resp[1]==0x03) throw new IOException("فشل نقل SMBus.");
                if(resp[1]!=0x02) continue;
                int n=resp[2]; if(n>got-3)n=(int)got-3;
                byte[] result=new byte[n]; Buffer.BlockCopy(resp,3,result,0,n); return result;
            }
            throw new TimeoutException("انتهت مهلة قراءة SMBus.");
        }

        public ushort ReadWord(byte command)
        {
            byte[] b=WriteRead(0x0B,command,2);
            if(b.Length<2) throw new IOException("رد البطارية غير مكتمل.");
            return (ushort)(b[0] | (b[1]<<8));
        }

        public short ReadSignedWord(byte command) { return unchecked((short)ReadWord(command)); }

        public string ReadText(byte command)
        {
            byte[] b=WriteRead(0x0B,command,32);
            if(b.Length==0)return "";
            int n=b[0]; if(n>b.Length-1)n=b.Length-1;
            return Encoding.ASCII.GetString(b,1,n).Trim('\0',' ');
        }

        public void Close()
        {
            if(handle!=INVALID){CloseHandle(handle);handle=INVALID;}
            path="";
        }
        public void Dispose(){Close();}
    }
}