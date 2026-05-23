using System.IO.Compression;

namespace MODUPDATER.Services;

public class ExtractService
{
    public void Extract(string zipPath, string extractPath)
    {
        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
    }
}
