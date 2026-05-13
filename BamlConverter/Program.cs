using System;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace BamlConverter;

class Program
{
    static void Main(string[] args)
    {
        string dllPath = args.Length > 0
            ? args[0]
            : @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\Extracted\baranggaysystem1.dll";

        string outputDir = args.Length > 1
            ? args[1]
            : @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\Decompiled";

        Console.WriteLine($"DLL: {dllPath}");
        Console.WriteLine($"Output: {outputDir}");

        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"ERROR: DLL not found: {dllPath}");
            return;
        }

        try
        {
            var module = new PEFile(dllPath);
            var resolver = new UniversalAssemblyResolver(dllPath, false, module.DetectTargetFrameworkId());
            
            // Add the directory containing the DLL to the resolver search paths
            var dllDir = Path.GetDirectoryName(dllPath)!;
            resolver.AddSearchDirectory(dllDir);

            var decompiler = new CSharpDecompiler(dllPath, resolver, new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false
            });

            // Get all resources from the assembly
            var resources = module.Resources.ToList();
            Console.WriteLine($"Found {resources.Count} resources in assembly");

            int xamlCount = 0;
            foreach (var resource in resources)
            {
                if (resource.ResourceType != ResourceType.Embedded)
                    continue;

                var name = resource.Name;
                Console.WriteLine($"  Resource: {name}");

                // WPF BAML resources are typically in a resource named like "assemblyname.g.resources"
                if (name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  -> Found WPF resource stream: {name}");
                    
                    using var stream = resource.TryOpenStream();
                    if (stream == null)
                    {
                        Console.WriteLine("    Could not open stream");
                        continue;
                    }

                    using var reader = new System.Resources.ResourceReader(stream);
                    var enumerator = reader.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var key = (string)enumerator.Key;
                        if (key.EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.Write($"    BAML: {key} ... ");
                            try
                            {
                                var xamlName = key.Replace(".baml", ".xaml");
                                var outputPath = Path.Combine(outputDir, xamlName);
                                var dir = Path.GetDirectoryName(outputPath)!;
                                Directory.CreateDirectory(dir);

                                // Get the BAML stream
                                reader.GetResourceData(key, out string typeName, out byte[] data);
                                
                                // The data from GetResourceData has a 4-byte length prefix for stream resources
                                byte[] bamlData;
                                if (data.Length > 4)
                                {
                                    int len = BitConverter.ToInt32(data, 0);
                                    if (len == data.Length - 4)
                                    {
                                        bamlData = new byte[len];
                                        Array.Copy(data, 4, bamlData, 0, len);
                                    }
                                    else
                                    {
                                        bamlData = data;
                                    }
                                }
                                else
                                {
                                    bamlData = data;
                                }

                                // Use ILSpy's BAML decompiler
                                using var bamlStream = new MemoryStream(bamlData);
                                var xaml = DecompileBaml(bamlStream, module, resolver, decompiler, CancellationToken.None);
                                
                                File.WriteAllText(outputPath, xaml);
                                Console.WriteLine("OK");
                                xamlCount++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"FAILED: {ex.Message}");
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"\nDone! Converted {xamlCount} BAML resources to XAML.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }

    static string DecompileBaml(Stream bamlStream, PEFile module, UniversalAssemblyResolver resolver, CSharpDecompiler decompiler, CancellationToken ct)
    {
        // Use ILSpy's BamlDecompiler via reflection or the built-in resource handling
        var bamlDecompilerType = typeof(CSharpDecompiler).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "XamlDecompiler");

        if (bamlDecompilerType != null)
        {
            // Try using the internal XamlDecompiler
            var method = bamlDecompilerType.GetMethod("Decompile", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                var result = method.Invoke(null, new object[] { bamlStream, ct });
                return result?.ToString() ?? "";
            }
        }

        // Fallback: Use the ICSharpCode.Decompiler's built-in BAML handling
        // The BamlDecompiler is in a separate namespace
        var asm = typeof(CSharpDecompiler).Assembly;
        var types = asm.GetTypes().Where(t => t.Name.Contains("Baml", StringComparison.OrdinalIgnoreCase)).ToList();
        
        Console.Write($"[Available BAML types: {string.Join(", ", types.Select(t => t.FullName))}] ");
        
        throw new NotSupportedException("Could not find BAML decompiler in ICSharpCode.Decompiler assembly");
    }
}
