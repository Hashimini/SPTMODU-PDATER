using System;
using System.IO;
using System.Collections.Generic;

namespace MODUPDATER.Services;

public class DeleteService
{
    public void ExecuteDeletion(string basePath, List<string>? relativePaths)
    {
        if (relativePaths == null || relativePaths.Count == 0) return;

        string fullBasePath = Path.GetFullPath(basePath);

        foreach (var relativePath in relativePaths)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath)) continue;

                string targetPath = Path.Combine(fullBasePath, relativePath);
                string fullTargetPath = Path.GetFullPath(targetPath);

                if (!fullTargetPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
                { continue; }

                if (File.Exists(fullTargetPath))
                {
                    File.Delete(fullTargetPath);
                }
                else if (Directory.Exists(fullTargetPath))
                {
                    Directory.Delete(fullTargetPath, true);
                }
            }
            catch (Exception) { }
        }
    }
}
