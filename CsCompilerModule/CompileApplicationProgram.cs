using System;
 
public class CompileApplicationProgram
{

    public static void Run(string[] args)
    {          
        var program = new CompileApplicationSources(@"D:\app1");
        program.AddClass(
            "WwwClient", 
           @"public class ProgramTest
            {
                public static void Main(){
                    System.Console.WriteLine(""this is a successfull compiled program"");
                }
            }
        "); 
        program.Run();
    }
}
 