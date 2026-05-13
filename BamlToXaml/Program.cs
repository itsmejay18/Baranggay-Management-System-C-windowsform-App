using System;
using System.Collections;
using System.IO;
using System.Resources;
using Confuser.Renamer.BAML;
using ICSharpCode.Decompiler.Metadata;

namespace BamlToXaml;

class Program
{
    static void Main(string[] args)
    {
        string dllPath = @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\Extracted\baranggaysystem1.dll";
        string outputDir = @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\Decompiled";

        if (args.Length > 0) dllPath = args[0];
        if (args.Length > 1) outputDir = args[1];

        Console.WriteLine($"DLL: {dllPath}");
        Console.WriteLine($"Output: {outputDir}");
        Console.WriteLine();

        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"ERROR: DLL not found: {dllPath}");
            return;
        }

        var module = new PEFile(dllPath);
        int success = 0, failed = 0;

        foreach (var resource in module.Resources)
        {
            if (resource.ResourceType != ResourceType.Embedded)
                continue;
            if (!resource.Name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase))
                continue;

            Console.WriteLine($"Processing: {resource.Name}");

            using var stream = resource.TryOpenStream();
            if (stream == null) continue;

            using var reader = new ResourceReader(stream);
            foreach (DictionaryEntry entry in reader)
            {
                var key = (string)entry.Key;
                if (!key.EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
                    continue;

                var xamlName = key.Replace(".baml", ".xaml");
                var outputPath = Path.Combine(outputDir, xamlName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                Console.Write($"  {key} -> ");
                try
                {
                    Stream bamlStream;
                    if (entry.Value is Stream s)
                    {
                        bamlStream = s;
                    }
                    else
                    {
                        reader.GetResourceData(key, out _, out byte[] data);
                        int len = BitConverter.ToInt32(data, 0);
                        if (len > 0 && len == data.Length - 4)
                            bamlStream = new MemoryStream(data, 4, len);
                        else
                            bamlStream = new MemoryStream(data);
                    }

                    var doc = BamlReader.ReadDocument(bamlStream);
                    var xaml = BamlToXamlConverter.Convert(doc);
                    File.WriteAllText(outputPath, xaml);
                    Console.WriteLine("OK");
                    success++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED: {ex.Message}");
                    failed++;
                }
            }
        }

        Console.WriteLine($"\nDone! Success: {success}, Failed: {failed}");
    }
}
