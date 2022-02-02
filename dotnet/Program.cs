using System;
using System.IO;

namespace dotnet
{
    class Program
    {
        private static string readme = File.ReadAllText("../README.md");
        static void Main(string[] args)
        {
            Console.WriteLine(readme);
        }
    }
}
