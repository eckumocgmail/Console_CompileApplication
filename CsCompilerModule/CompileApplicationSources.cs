using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
 

public class CompileApplicationSources
{
    private IDictionary<string, byte[]> Sources;
    private List<string> Libs;
    private string Directory;

    public CompileApplicationSources(string Directory = null)
    {
        this.Sources = new ConcurrentDictionary<string, byte[]>();
        this.Libs = new List<string>();
        this.Directory = Directory != null ? Directory
            : System.IO.Directory.GetCurrentDirectory();            
    }

    public void Run() => Run(Exe());
    public bool Run(byte[] package)
    {
        var loaded = System.Reflection.Assembly.Load(package);
        loaded.EntryPoint.Invoke(null, new object[0]);
        return true;
    }


    /// <summary>
    /// Сохранение исходных файлов 
    /// </summary>        
    private void Save(string projectDirectory)
    {
        foreach (var Src in Sources)
        {
            string FileName = Src.Key;
            byte[] FileData = Src.Value;
            CreateTextFile(projectDirectory, FileName + ".cs", FileData);
        }
    }

    public Type[] AddClass(string key, string source)
    {
        this.Sources.Add(key, Encoding.UTF8.GetBytes(source));
        byte[] dll = Exe();

        return Assembly.Load(dll).GetTypes();
    }


    /// <summary>
    /// Выполнение инструкции через командную строку
    /// </summary>
    /// <param name="command"> команда </param>
    /// <returns></returns>
    public string CmdExec(string command)
    {
        command = ReplaceAll(ReplaceAll(ReplaceAll(ReplaceAll(command, "\n", ""), "\r", ""), @"\\", @"\"), @"//", @"/");
        Console.WriteLine(command);
        ProcessStartInfo info = new ProcessStartInfo("CMD.exe", "/C " + command);

        info.RedirectStandardError = true;
        info.RedirectStandardOutput = true;
        info.UseShellExecute = false;
        System.Diagnostics.Process process = System.Diagnostics.Process.Start(info);
        string response = process.StandardOutput.ReadToEnd();
        string result = ReplaceAll(response, "\r", "\n");
        result = ReplaceAll(result, "\n\n", "\n");
        while (result.EndsWith("\n"))
        {
            result = result.Substring(0, result.Length - 1);
        }
        return result;
    }


    /// <summary>
    /// возвращает путь к exe-файлу
    /// </summary>        
    public byte[] Exe()
    {
        this.Save(this.Directory);
        this.Compile();
        byte[] data = this.Read(GetBinPath());
        Assembly intagrated = Assembly.Load(data);
        foreach (var type in intagrated.GetTypes())
        {
            Console.WriteLine(type.FullName);
        }
        this.ClearDirectory();

        return data;
    }

    /// <summary>
    /// Отчистка каталога
    /// </summary>
    private void ClearDirectory()
    {
        foreach (var file in System.IO.Directory.GetFiles(Directory))
            System.IO.File.Delete(file);
        foreach (var file in System.IO.Directory.GetDirectories(Directory))
            System.IO.File.Delete(file);
    }

    /// <summary>
    /// путь к скомпилированному exe
    /// </summary>   
    private string GetBinPath()
        => System.IO.Directory.GetFiles(Directory, "*.exe", SearchOption.AllDirectories).FirstOrDefault();

    /// считывание бинарных данных         
    private byte[] Read(string path)
        => System.IO.File.ReadAllBytes(path);


    private AppConfig Config = new AppConfig();


    class AppConfig : Dictionary<string, string>
    {

    }


    /// <summary>
    /// Сохраняет конфигурацию сборки
    /// </summary>
    private void SaveConfig(string ProjectDirectory)
    {
        CreateStringToTextFile(ProjectDirectory, "appconfig.cson",
        @"<Project Sdk=""Microsoft.NET.Sdk"">

            <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net5.0</TargetFramework>
                <RunPostBuildEvent>Always</RunPostBuildEvent>
            </PropertyGroup>

        </Project>"
        );
    }

    /// <summary>
    /// Компиляция csc.exe
    /// </summary>
    private void Compile()
    {

        SaveConfig(Directory);
        Save(Directory);

        string cmd = "";
        foreach (var key in Sources.Keys)
        {
            cmd += "," + key + ".cs";
        }
        if (cmd.StartsWith(","))
            cmd = cmd.Substring(1);

        //cmd += " -main:Www.System"; // невкл. стадарт библиотку в сборку
                                    //cmd += " -nostdlib+"; // невкл. стадарт библиотку в сборку
                                    //cmd += " -appconfig:appconfig.cson"; // исп. дополнительную защиту ASLR (устраняет уязвимость в программе)
                                    //cmd += " -highentropyva:+"; // исп. дополнительную защиту ASLR (устраняет уязвимость в программе)
        cmd = $"cd {Directory} && csc.exe " + cmd;
        string output = CmdExec(cmd);

        Console.WriteLine(output);
    }



    private static void CreateStringToTextFile(string ProjectDirectory, string FileName, string FileData)
    {
        string absolutelyPath = Path.Combine(ProjectDirectory, FileName);
        string path = absolutelyPath;
        string cursor = path.Substring(0, 2);
        var arr = path.Split("\\");
        for (int i = 1; i < arr.Length; i++)
        {
            var name = arr[i];
            cursor += $"\\{name}";
            if (name.Contains('.') == false)
            {
                System.IO.Directory.CreateDirectory(cursor);
            }
            else
            {
                using (var writer = System.IO.File.CreateText(cursor))
                {
                    string text = FileData;
                    writer.WriteLine(text);
                    writer.Flush();
                }
            }
        }
    }




    private void CreateTextFile(string ProjectDirectory, string FileName, byte[] FileData)
    {
        
            string absolutelyPath = Path.Combine(ProjectDirectory, FileName);
            string path = absolutelyPath;
            string cursor = path.Substring(0, 2);
            var arr = path.Split("\\");
            for (int i = 1; i < arr.Length; i++)
            {
                var name = arr[i];
                cursor += $"\\{name}".Replace(@"\\",@"\");
                try
                {
                    if (name == "net6.0" || name == "net5.0" || name.Contains('.') == false)
                    {
                        System.IO.Directory.CreateDirectory(cursor);
                    }
                    else
                    {
                        using (var writer = System.IO.File.Create(cursor))
                        {
                            writer.Write(FileData, 0, FileData.Length);
                            writer.Flush();
                        }
                    }
            }
                catch (Exception ex)
                {
                    throw new Exception("Ошибка при сохранении файла "+ cursor, ex);
                }
                
            }
        
    }

    /// <summary>
    /// Замена подстрок
    /// </summary>
    public static string ReplaceAll(string text, string s1, string s2)
    {
        string p = text;
        while (p.IndexOf(s1) != -1)
        {
            p = p.Replace(s1, s2);
        }
        return p;
    }
}

    

    
    
    
    




 