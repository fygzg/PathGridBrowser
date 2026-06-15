// File: Program.cs
// 应用程序入口点

using System;
using System.Windows.Forms;

namespace DirectoryGridBrowser
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}