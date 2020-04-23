using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using IronPython.Hosting;
using IronPython.Runtime;
using IronPython;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.IO;
using Windows.Storage;

using System.Runtime.InteropServices;



namespace FullTrust
{

    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();


        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        static void Main(string[] args)

        {
            const int SW_HIDE = 0;
            const int SW_SHOW = 5;
            var handle = GetConsoleWindow();

            //ShowWindow(handle, SW_HIDE); // To hide
            //Console.WindowHeight = 100;
            //string fileName = "C:\\Users\\Danyal Tariq\\source\\repos\\UWP-FullTrust\\UWP_FullTrust_1\\Package\\bin\x64\\debug\\FullTrust\\Loader.py";

            //Process p = new Process();
            //p.StartInfo = new ProcessStartInfo(@"E:\Anaconda3\python.exe")
            //{
            //    RedirectStandardOutput = true,
            //    UseShellExecute = false,
            //    CreateNoWindow = false
            //};
            //p.Start();

            //string output = p.StandardOutput.ReadToEnd();
            //p.WaitForExit();

            //Console.WriteLine(output);

            //Console.ReadLine();

            string parameters = ApplicationData.Current.LocalSettings.Values["parameters"] as string;

            Console.WriteLine(parameters);
            
            var process = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                Arguments=parameters,
                RedirectStandardInput = true,
                RedirectStandardOutput= true,
                UseShellExecute = false,
                CreateNoWindow=false,
                WorkingDirectory=@"C:\Users\Danyal Tariq\source\repos\UWP-FullTrust\UWP_FullTrust_1\Package\bin\x64\debug\FullTrust\"
            };

            process.StartInfo = startInfo;
            process.Start();

            process.StandardInput.WriteLine(@"echo on");
            process.StandardInput.WriteLine($"run.bat {parameters}>output.txt");
            //process.StandardInput.WriteLine($"Loader.py NIGGA >output.txt");
            //Console.ReadKey();
        }

    }

}
