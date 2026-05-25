using System.IO.Compression;

namespace MODUPDATER.Services;

public class ExtractService
{
    // Extremelly simple to extract thanks to System.IO.Compression
    public void Extract(string zipPath, string extractPath)
    {
        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
    }
}
