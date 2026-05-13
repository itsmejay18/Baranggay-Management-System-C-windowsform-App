using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string exePath = @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\BarangayManagementSystem.exe";
        string outputDir = @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\Extracted";
        string logFile = @"C:\Users\ADMIN\Desktop\files\dvo\Baranggay-Management-System-C-windowsform-App\extraction_log.txt";
        
        Directory.CreateDirectory(outputDir);
        using var writer = new StreamWriter(logFile);
        void Log(string msg) { writer.WriteLine(msg); writer.Flush(); }

        byte[] fileBytes = File.ReadAllBytes(exePath);
        Log($"File size: {fileBytes.Length} bytes");
        
        // From the previous analysis, we know the v6 bundle entry for baranggaysystem1.dll:
        // The name "baranggaysystem1.dll" was found at offset 97765353
        // Entry format: offset(8) + size(8) + compressedSize(8) + type(1) + nameLen(1) + name
        // Entry starts at: 97765353 - 20(name) - 1(varint) - 1(type) - 24(fields) = 97765307
        // Values: offset=15952175, size=2080768, compressed=551984
        
        // Let's extract ALL entries by finding all file names in the manifest region
        // The manifest is roughly from offset 97600000 to end of file
        
        Log("=== Finding all bundle entries ===");
        
        // Strategy: find all entry names by looking for the pattern of valid entries
        // We'll scan forward through the manifest region
        
        // First, let's find where the manifest starts by looking for the first entry
        // We know entries are packed sequentially. Let's find the boundary.
        
        // From the backwards parse, the earliest entry found was System.Data.SqlClient.dll at offset 85790682
        // The manifest entry for it starts somewhere before that in the manifest area
        
        // Better approach: find ALL occurrences of ".dll" and ".json" in the last portion of the file
        // that look like they're part of bundle entries
        
        // Actually, let's just directly extract baranggaysystem1.dll using the known values
        Log("=== Extracting baranggaysystem1.dll ===");
        long dllOffset = 15952175;
        long dllSize = 2080768;
        long dllCompressed = 551984;
        
        Log($"  offset={dllOffset}, uncompressed_size={dllSize}, compressed_size={dllCompressed}");
        
        byte[] compData = new byte[dllCompressed];
        Array.Copy(fileBytes, (int)dllOffset, compData, 0, (int)dllCompressed);
        
        Log($"  First bytes of compressed data: {BitConverter.ToString(compData, 0, 8)}");
        
        // Try deflate decompression
        try
        {
            using var ms = new MemoryStream(compData);
            using var ds = new DeflateStream(ms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            ds.CopyTo(outMs);
            byte[] dllData = outMs.ToArray();
            
            Log($"  Decompressed: {dllData.Length} bytes (expected {dllSize})");
            
            if (dllData.Length > 2 && dllData[0] == 0x4D && dllData[1] == 0x5A)
            {
                Log("  SUCCESS! Valid PE file!");
                File.WriteAllBytes(Path.Combine(outputDir, "baranggaysystem1.dll"), dllData);
            }
            else
            {
                Log($"  Not MZ. First bytes: {BitConverter.ToString(dllData, 0, Math.Min(16, dllData.Length))}");
                File.WriteAllBytes(Path.Combine(outputDir, "baranggaysystem1.dll.raw"), dllData);
            }
        }
        catch (Exception ex)
        {
            Log($"  Deflate failed: {ex.Message}");
            Log("  Trying raw (uncompressed)...");
            
            // Maybe it's not compressed despite having a compressedSize value
            byte[] rawData = new byte[dllSize];
            Array.Copy(fileBytes, (int)dllOffset, rawData, 0, (int)dllSize);
            
            if (rawData[0] == 0x4D && rawData[1] == 0x5A)
            {
                Log("  Raw data starts with MZ! Saving...");
                File.WriteAllBytes(Path.Combine(outputDir, "baranggaysystem1.dll"), rawData);
            }
            else
            {
                Log($"  Raw first bytes: {BitConverter.ToString(rawData, 0, 16)}");
                File.WriteAllBytes(Path.Combine(outputDir, "baranggaysystem1.dll.raw"), rawData);
            }
        }
        
        // Also try to find and extract the runtimeconfig.json
        Log("\n=== Looking for runtimeconfig.json ===");
        string ascii = Encoding.ASCII.GetString(fileBytes);
        int rcIdx = ascii.LastIndexOf("baranggaysystem1.runtimeconfig.json");
        if (rcIdx > 0)
        {
            Log($"  Found name at offset {rcIdx}");
            int rcNameLen = 35; // "baranggaysystem1.runtimeconfig.json"
            int rcEntry = rcIdx - 1 - 1 - 24;
            long rcOff = BitConverter.ToInt64(fileBytes, rcEntry);
            long rcSize = BitConverter.ToInt64(fileBytes, rcEntry + 8);
            long rcComp = BitConverter.ToInt64(fileBytes, rcEntry + 16);
            Log($"  offset={rcOff}, size={rcSize}, compressed={rcComp}");
            
            if (rcOff > 0 && rcOff < fileBytes.Length && rcSize > 0 && rcSize < 10_000_000)
            {
                long rcActual = (rcComp > 0 && rcComp != rcSize) ? rcComp : rcSize;
                byte[] rcData = new byte[rcActual];
                Array.Copy(fileBytes, (int)rcOff, rcData, 0, (int)rcActual);
                
                if (rcComp > 0 && rcComp != rcSize)
                {
                    try
                    {
                        using var ms = new MemoryStream(rcData);
                        using var ds = new DeflateStream(ms, CompressionMode.Decompress);
                        using var outMs = new MemoryStream();
                        ds.CopyTo(outMs);
                        rcData = outMs.ToArray();
                    }
                    catch { }
                }
                
                File.WriteAllBytes(Path.Combine(outputDir, "baranggaysystem1.runtimeconfig.json"), rcData);
                Log($"  Extracted ({rcData.Length} bytes)");
            }
        }
        
        // Now try to decompile the DLL if it was extracted successfully
        string dllPath = Path.Combine(outputDir, "baranggaysystem1.dll");
        if (File.Exists(dllPath))
        {
            var dllBytes = File.ReadAllBytes(dllPath);
            if (dllBytes[0] == 0x4D && dllBytes[1] == 0x5A)
            {
                Log($"\n=== baranggaysystem1.dll is valid ({dllBytes.Length} bytes) ===");
                Log("Ready for decompilation with: ilspycmd -p -o Decompiled baranggaysystem1.dll");
            }
        }
        
        Log("\nDone.");
    }
}
