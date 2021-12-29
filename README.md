# OnAccessScanDemo

1.	McAfee ENS has a new feature/behavior called SmoothWrites which is intended to improve performance by reducing scan overhead. This behavior is not present in the legacy VSE product.
2.	The SmoothWrites feature cannot be turned off or adjusted/configured, and is not documented through McAfee’s public support information.
3.	If a file is opened within 500ms by the same process for most file extensions, McAfee will not intercept file read activity. For specific extensions INI, XML, LOG and TXT, this delay before flagging scan is 2000ms.
4.	If the file is accessed by a different process, this is not smoothed and the read will be processed in accordance with the trust level of the accessing process. 
5.	In default On-Access Scan policy, the option to scan compressed archives files is disabled (similar to VSE). McAfee recommends enabling this policy setting to scan inside compressed files. 

This application demonstrates On-Access Scan behavior.

Tests that the application performs:
1.  Pass: Innocent file with separate process. Requires build of ReadOneByteUtility.exe
    1. Write innocent file to random .txt filename, close file handle
    1. Run process ReadOneByteUtility.exe
    1. Check exit code of process, verify that it was able to read one byte
1.  Pass: EICAR file with separate process. Requires build of ReadOneByteUtility.exe
    1. Write EICAR file to random .com filename, close file handle
    1. Run process ReadOneByteUtility.exe
    1. Check exit code of process, verify that it was unable to read one byte
1.	Pass: Small EICAR
    1.	Write EICAR test file to random .com filename, close file handle
    1.	Sleep 500ms
    1.	Read back first byte, which should fail due to on access scan
2.	Pass: EICAR with INI extension
    1.	Write EICAR test file to random .ini filename, close file handle
    1.	Sleep 2000ms
    1.	Read back first byte, which should fail due to on access scan
3.	Pass: Plain text innocent file
    1.	Write random file content to random .com filename, close file handle
    1.	Sleep 500ms
    1.	Read back first byte, which should succeed as it should not be detected
4.	Pass: Small zip file with embeddedeicar.com file. Requires that scanning inside compressed files is turned on.
    1.	Write zip file with an entry embeddedeicar.com file, containing EICAR test content, close file handle
    1.	Sleep 500ms
    1.	Read the zip file, including reading the first byte of the zip entry of embeddedEicar.com, which should fail due to on access scan
5.	Pass: Large zip file with embeddedeicar.com file. Requires that scanning inside compressed files is turned on.
    1.	Write zip file with an entry embeddedeicar.com file, containing EICAR test content and an additional zip entry of 100 MB, close file handle
    2.	Sleep 500ms
    1.	Read the zip file, including reading the first byte of the zip entry of embeddedEicar.com, which should fail due to on access scan

To build the byte utility, run cl /W4 /EHsc ReadOneByteUtility.cpp from a Visual Studio Developer Command Line