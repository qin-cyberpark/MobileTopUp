using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MobileTopUp.Utilities;

namespace MobileTopUp.Tests.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = @"e:\temp\1.jpg";
            System.Console.WriteLine("Scanning " + file);
            string text = OCRHelper.ScanVoucherNumber(file);
            System.Console.Write(text);
            System.Console.WriteLine("== scanned ==");
            System.Console.ReadLine();
        }
    }
}
