// Program.cs
using System;
using System.Windows.Forms;

namespace GomokuGame
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm()); // 启动主窗体
        }
    }
}
