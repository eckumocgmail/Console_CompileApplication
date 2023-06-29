using System;
using System.Linq;
using System.Reflection;

public class CompileApplicationProgram
{
    public static void Run(string[] args)
    {
        var sources = new CompileApplicationSources(@"D:\System-Config\Temp");
        sources.AddClass("Program.cs", @"
            class Program
            {
                public static void Main(string[] args)
                {
                    System.Console.WriteLine(1);
                }
            }
        ");
        sources.Exe();
    }
}
 