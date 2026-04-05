using System;

namespace TM_MappingTools.Utils;

public static class FileHelper
{
    public const long MaxAllowedFileSize = 500 * 1024 * 1024; // 500 MB

    public static string GetFormattedFileSize(long fileSize)
    {
        if (fileSize < 1024) return $"{fileSize} B";
        if (fileSize < 1024 * 1024) return $"{fileSize / 1024.0:F1} KB";
        return $"{fileSize / 1024.0 / 1024.0:F1} MB";
    }
}
