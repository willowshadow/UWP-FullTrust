﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FullTrust
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Hello World";
            Console.WriteLine("This process runs at the full privileges of the user and has access to the entire public desktop API surface");
            Console.WriteLine("\r\nPress any key to exit ...");
            Console.ReadLine();
            Process process = new Process();
            process.StartInfo.FileName = "E:\\Anaconda\\python.exe";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Arguments = "Loader.py";
            process.Start();
            
        }
    }
}
