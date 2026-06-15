// File: SessionStorage.cs
// 保存/恢复上次关闭时的格子布局与目录

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DirectoryGridBrowser
{
    internal class AppSession
    {
        public int GridCount { get; set; } = 4;
        public List<string> Directories { get; set; } = new();
        public List<float> ColumnWidths { get; set; } = new();
        public List<float> RowHeights { get; set; } = new();
    }

    internal static class SessionStorage
    {
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DirectoryGridBrowser",
            "session.json");

        public static AppSession? Load()
        {
            try
            {
                if (!File.Exists(SessionFilePath))
                    return null;

                string json = File.ReadAllText(SessionFilePath);
                return JsonSerializer.Deserialize<AppSession>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(AppSession session)
        {
            try
            {
                string? dir = Path.GetDirectoryName(SessionFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SessionFilePath, json);
            }
            catch
            {
                // 保存失败不影响退出
            }
        }
    }
}
