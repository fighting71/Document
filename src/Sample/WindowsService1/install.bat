%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe G:\dick\git\Document\src\Sample\WindowsService1\bin\Debug\WindowsService1.exe
Net Start WindowsServiceTest
sc config WindowsServiceTest start= auto
pause