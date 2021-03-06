using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace OnAccessScanDemo
{
    class Program
    {
        static int timeToWaitMs = 500;
        static int iniTimeToWaitMs = 2000;
        static bool cleanupTempFiles = false;
        static string basePath = @"c:\sandboxMcAfee";
        static int largeFileThresholdMb = 100;
        /*
         * Demonstrates On-Access scanning from McAfee by round-tripping the EICAR file to disk
         * https://service.mcafee.com/webcenter/portal/oracle/webcenter/page/scopedMD/s55728c97_466d_4ddb_952d_05484ea932c6/Page29.jspx?wc.contextURL=%2Fspaces%2Fcp&articleId=TS100829&_afrLoop=330580533484445&leftWidth=0%25&showFooter=false&showHeader=false&rightWidth=0%25&centerWidth=100%25#!%40%40%3FshowFooter%3Dfalse%26_afrLoop%3D330580533484445%26articleId%3DTS100829%26leftWidth%3D0%2525%26showHeader%3Dfalse%26wc.contextURL%3D%252Fspaces%252Fcp%26rightWidth%3D0%2525%26centerWidth%3D100%2525%26_adf.ctrl-state%3Dolsadloaf_9
         * */
        static int Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if(File.Exists("ReadOneByteUtility.exe"))
            {
                Console.WriteLine($"--- Writing innocent file with separate process and no wait ---");
                var newInnocentFileName = WriteInnocentFile().GetAwaiter().GetResult();
                Process innocentUtilityProcess = System.Diagnostics.Process.Start("ReadOneByteUtility.exe", newInnocentFileName);
                innocentUtilityProcess.WaitForExit();
                int exitCode = innocentUtilityProcess.ExitCode;
                sw.Stop();
                WriteTestResult(exitCode == 0, "Check innocent file with separate process");
                Console.WriteLine($"Time taken: {sw.ElapsedMilliseconds}ms");
                Console.WriteLine();

                sw.Start();
                Console.WriteLine($"--- Writing small EICAR file with separate process and no wait ---");
                var newEicarFileName = WriteEicarFile(".com").GetAwaiter().GetResult();
                Process utilityProcess = System.Diagnostics.Process.Start("ReadOneByteUtility.exe", newEicarFileName);
                utilityProcess.WaitForExit();
                exitCode = utilityProcess.ExitCode;
                sw.Stop();
                WriteTestResult(exitCode != 0, "Detect in small EICAR file with separate process");
                Console.WriteLine($"Time taken: {sw.ElapsedMilliseconds}ms");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("ReadOneByteUtility.exe missing, skipping separate process tests. Review Readme.md for information on how to build.");
                Console.WriteLine();
            }

            sw.Reset();
            sw.Start();
            Console.WriteLine($"--- Writing small EICAR file ---");
            var eicarFileName = WriteEicarFile(".com").GetAwaiter().GetResult();
            Console.WriteLine($"Sleeping {timeToWaitMs}ms");
            System.Threading.Thread.Sleep(timeToWaitMs);
            var isEicarVirusDetected = IsVirusDetectedOrRemoved(eicarFileName).GetAwaiter().GetResult();
            sw.Stop();
            WriteTestResult(isEicarVirusDetected, "Detect virus in small EICAR file");
            Console.WriteLine($"Time taken: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine();

            sw.Reset();
            sw.Start();
            Console.WriteLine($"--- Writing small EICAR file with INI extension ---");
            var logEicarFileName = WriteEicarFile(".ini").GetAwaiter().GetResult();
            Console.WriteLine($"Sleeping {iniTimeToWaitMs}ms");
            System.Threading.Thread.Sleep(iniTimeToWaitMs);
            var isLogEicarVirusDetected = IsVirusDetectedOrRemoved(logEicarFileName).GetAwaiter().GetResult();
            sw.Stop();
            WriteTestResult(isLogEicarVirusDetected, "Detect virus in small EICAR file with INI extension");
            Console.WriteLine($"Time taken: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine();

            Console.WriteLine($"--- Writing plain text innocent file ---");
            var innocentFileName = WriteInnocentFile().GetAwaiter().GetResult();
            Console.WriteLine($"Sleeping {iniTimeToWaitMs}ms");
            System.Threading.Thread.Sleep(iniTimeToWaitMs);
            var isInnocentVirusDetected = IsVirusDetectedOrRemoved(innocentFileName).GetAwaiter().GetResult();
            WriteTestResult(!isInnocentVirusDetected, "Read innocent file");
            Console.WriteLine();

            Console.WriteLine($"--- Writing small zip file with embedded EICAR ---");
            var zippedEicarFile = WriteZippedEicarFile(false).GetAwaiter().GetResult();
            Console.WriteLine($"Sleeping {timeToWaitMs}ms");
            System.Threading.Thread.Sleep(timeToWaitMs);
            var isZippedEicarDetected = IsVirusDetectedOrRemovedInZip(zippedEicarFile).GetAwaiter().GetResult();
            WriteTestResult(isZippedEicarDetected, "Detect virus in small zip file (requires McAfee setting)");

            Console.WriteLine();

            Console.WriteLine($"--- Writing large zip file with embedded EICAR ---");
            var largeZippedEicarFile = WriteZippedEicarFile(true).GetAwaiter().GetResult();
            Console.WriteLine($"Sleeping {timeToWaitMs}ms");
            System.Threading.Thread.Sleep(timeToWaitMs); 
            var isLargeZippedEicarDetected = IsVirusDetectedOrRemovedInZip(largeZippedEicarFile).GetAwaiter().GetResult();
            WriteTestResult(isLargeZippedEicarDetected, "Detect virus in large zip file (requires McAfee setting)");


            if (cleanupTempFiles)
            { 
                DeleteFile(eicarFileName);
                DeleteFile(innocentFileName);
                DeleteFile(zippedEicarFile);
            }
            return 0;
        }

        private static void WriteTestResult(bool isSuccess, string testName)
        {
            if(isSuccess)
            {
                Console.WriteLine($"PASS: {testName}");
            }
            else
            {
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAIL: ");
                Console.ResetColor();
                Console.WriteLine($"{testName}");
                
            }
        }

        private static void DeleteFile(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch(Exception)
            {
                return;
            }
        }
        private static string GetTestFileName(string extension)
        {
            string randomFilename = Path.GetRandomFileName();
            int indexOfExtension = randomFilename.IndexOf(".");
            if(indexOfExtension > 0)
                randomFilename = randomFilename.Substring(0, indexOfExtension - 1);

            if (!extension.StartsWith("."))
                extension = $".{extension}";

            randomFilename = $"{randomFilename}{extension}";
            return Path.Combine(basePath, randomFilename);
        }

        private static async Task<string> WriteInnocentFile()
        {
            string eicarFileContent = @"I AM AN INNOCENT FILE";
            return await WriteFileContents(eicarFileContent, ".txt");
        }
        private static async Task<string> WriteEicarFile(string fileExtension)
        {
            Random randomBytes = new Random();
            string eicarFileContent = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*" + randomBytes.Next().ToString();
            return await WriteFileContents(eicarFileContent, fileExtension);
        }
        private static async Task<string> WriteZippedEicarFile(bool largeZip)
        {
            Random randomBytes = new Random();
            string eicarFileContent = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*" + randomBytes.Next().ToString();
            byte[] bytesToWrite = System.Text.Encoding.ASCII.GetBytes(eicarFileContent);
            return await WriteZipFileContents(bytesToWrite, "embeddedEicar.com", largeZip);
        }
        private static async Task<string> WriteFileContents(string dataToWrite, string fileExtension)
        {
            byte[] bytesToWrite = System.Text.Encoding.ASCII.GetBytes(dataToWrite);
            return await WriteFileContents(bytesToWrite, fileExtension);
        }
        private static async Task<string> WriteFileContents(byte[] dataToWrite, string fileExtension)
        {
            string tempFileName = GetTestFileName(fileExtension);
            using (FileStream writeFileContents = File.Open(tempFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                await writeFileContents.WriteAsync(dataToWrite, 0, dataToWrite.Length);
                await writeFileContents.FlushAsync();
                writeFileContents.Close();
            }
            return tempFileName;
        }

        private static async Task<string> WriteZipFileContents(byte[] dataToWrite, string fileNameOfBytes, bool largeZip)
        {
            string tempFileName = GetTestFileName(".zip");
            using (FileStream writeFileContents = File.Open(tempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                using (ZipArchive archive = new ZipArchive(writeFileContents, ZipArchiveMode.Update))
                {
                    if (largeZip)
                    {
                        ZipArchiveEntry largeEntry = archive.CreateEntry("largePaddedContents.txt");
                        using (var writeStream = largeEntry.Open())
                        {
                            Random randomBytes = new Random();
                            byte[] randomBuffer = new byte[1000000];
                            
                            for(int i = 0; i < largeFileThresholdMb; i++)
                            {
                                randomBytes.NextBytes(randomBuffer);
                                await writeStream.WriteAsync(randomBuffer, 0, randomBuffer.Length);
                            }
                        }
                    }

                    ZipArchiveEntry readmeEntry = archive.CreateEntry(fileNameOfBytes);
                    using (var writeStream = readmeEntry.Open())
                    {
                        await writeStream.WriteAsync(dataToWrite, 0, dataToWrite.Length);
                    }
                }
            }
            return tempFileName;
        }

        /*
        * This method determines if the file exists (if not, probably swept by the On Access Scanner already) 
        * and if there is an access exception (likely On Access Scanner blocked the local file read)
        * The code leverages this behavior of the McAfee On Access Scanner, which hooks directly into the file read operation of the OS:
        * https://docs.mcafee.com/bundle/endpoint-security-10.5.0-threat-prevention-product-guide-epolicy-orchestrator-windows/page/GUID-5A870D4E-FFBB-4F32-866E-A0F26F327501.html
        * 
        * https://kc.mcafee.com/corporate/index?page=content&id=KB85136
        * */
        private static async Task<bool> IsVirusDetectedOrRemoved(string tempFileName)
        {

            Console.WriteLine($"Checking if file exists: {tempFileName}");
            // On Access Scanner may have already detected the file, optimize the file check to avoid throwing exception
            bool fileExists = File.Exists(tempFileName);
            if (!fileExists)
                return true;
            try
            { 
                byte[] readFirstByte = new byte[1];
                // typically the File.OpenRead() would throw UnauthorizedAccess exception
                using FileStream readStream = File.OpenRead(tempFileName);
                // read first byte to make sure that the file is readable
                await readStream.ReadAsync(readFirstByte);
            }
            catch(UnauthorizedAccessException)
            {
                // the file isn't authorized: On Access Scanner (or the OS) blocked the file access
                return true;
            }
            catch(FileNotFoundException)
            {
                // the file isn't present: On Access Scanner may have swept it already
                return true;
            }
            // no exception found, the virus is not detected/file removed
            return false;
        }

        /*
         * https://mytecharea.wordpress.com/2017/07/08/read-the-content-of-a-file-in-a-zip-file-without-extracting-the-file-in-c/
         * */
        private static async Task<bool> IsVirusDetectedOrRemovedInZip(string tempFileName)
        {
            Console.WriteLine($"Checking if file exists: {tempFileName}");
            // On Access Scanner may have already detected the file, optimize the file check to avoid throwing exception
            bool fileExists = File.Exists(tempFileName);
            if (!fileExists)
                return true;
            try
            {
                byte[] readFirstByte = new byte[1];
                // typically the File.OpenRead() would throw UnauthorizedAccess exception
                using FileStream readStream = new FileStream(tempFileName, FileMode.Open);
                using var archive = new ZipArchive(readStream, ZipArchiveMode.Read);
                foreach (var entry in archive.Entries)
                {
                    using var entryStream = entry.Open();
                    await entryStream.ReadAsync(readFirstByte);
                }

            }
            catch (UnauthorizedAccessException)
            {
                // the file isn't authorized: On Access Scanner (or the OS) blocked the file access
                return true;
            }
            catch (FileNotFoundException)
            {
                // the file isn't present: On Access Scanner may have swept it already
                return true;
            }
            // no exception found, the virus is not detected/file removed
            return false;
        }
    }
}
