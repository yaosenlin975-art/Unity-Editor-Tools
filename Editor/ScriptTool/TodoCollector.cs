/*
┌────────────────────────────┐
│　Description: 
│　Remark: 
└────────────────────────────┘
*/

using Lin.Editor.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Lin.Editor.ScriptTool
{
    public static class TodoCollector
    {
        [MenuItem("Lin/脚本/收集TODO")]
        public static void CollectByMenu()
        {
            CollectInternal();
        }

        [DidReloadScripts]
        private static void Collect()
        {
            CollectInternal();
        }

        private static void CollectInternal()
        {
            string docsFolder = EditorAnnotationSettings.DocsOutputFolderName;
            string projectRoot = new DirectoryInfo(Application.dataPath).Parent.FullName;
            string outputPath = Path.Combine(projectRoot, docsFolder, "Todos.md");

            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            var scriptPaths = new List<string>(scriptGuids.Length);
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Assets\\", StringComparison.Ordinal))
                    continue;

                if (ShouldIgnorePath(path))
                    continue;

                scriptPaths.Add(path);
            }

            Task.Run(() =>
            {
                try
                {
                    var results = CollectTodosInBackground(projectRoot, scriptPaths);
                    string content = BuildMarkdown(results);
                    WriteMarkdown(content, outputPath);

                    EditorApplication.delayCall += () =>
                        Debug.Log($"[TodoCollector] 已生成: {docsFolder}/Todos.md (共 {results.Count} 条)");
                }
                catch (Exception e)
                {
                    EditorApplication.delayCall += () =>
                        Debug.LogError($"[TodoCollector] {e.Message}");
                }
            });
        }

        private static bool ShouldIgnorePath(string path)
        {
            var ignoreFolders = EditorAnnotationSettings.TodoIgnoreFolders;
            if (ignoreFolders != null)
            {
                foreach (var folder in ignoreFolders)
                {
                    if (!string.IsNullOrEmpty(folder) && path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            var ignoreAssemblies = EditorAnnotationSettings.TodoIgnoreAssemblies;
            if (ignoreAssemblies != null)
            {
                string assemblyName = GetAssemblyNameFromPath(path);
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    foreach (var assembly in ignoreAssemblies)
                    {
                        if (!string.IsNullOrEmpty(assembly) && assemblyName.Equals(assembly, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        private static string GetAssemblyNameFromPath(string path)
        {
            string normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.StartsWith("Packages/"))
            {
                int packageEnd = normalizedPath.IndexOf('/', 9);
                if (packageEnd > 0)
                {
                    string packagePath = normalizedPath.Substring(0, packageEnd);
                    string packageJsonPath = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, packagePath.Replace('/', Path.DirectorySeparatorChar), "package.json");
                    if (File.Exists(packageJsonPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(packageJsonPath);
                            var match = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
                            if (match.Success)
                                return match.Groups[1].Value;
                        }
                        catch { }
                    }
                }
            }

            string directory = Path.GetDirectoryName(path);
            while (!string.IsNullOrEmpty(directory))
            {
                string asmdefPath = FindAsmdefInDirectory(directory);
                if (!string.IsNullOrEmpty(asmdefPath))
                {
                    try
                    {
                        string json = File.ReadAllText(asmdefPath);
                        var match = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
                        if (match.Success)
                            return match.Groups[1].Value;
                    }
                    catch { }
                    break;
                }
                directory = Path.GetDirectoryName(directory);
            }

            return null;
        }

        private static string FindAsmdefInDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return null;

            var asmdefFiles = Directory.GetFiles(directory, "*.asmdef");
            if (asmdefFiles.Length > 0)
                return asmdefFiles[0];

            return null;
        }

        private static List<TodoInfo> CollectTodosInBackground(string projectRoot, List<string> scriptPaths)
        {
            var bag = new ConcurrentBag<TodoInfo>();
            Parallel.For(0, scriptPaths.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
            {
                string assetPath = scriptPaths[i];
                string fullPath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
                try
                {
                    ScanFileForTodos(fullPath, assetPath, bag);
                }
                catch
                {
                }
            });

            var list = new List<TodoInfo>(bag);
            list.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.scriptPath, b.scriptPath);
                if (c != 0)
                    return c;
                return a.line.CompareTo(b.line);
            });
            return list;
        }

        private static void ScanFileForTodos(string fullPath, string assetPath, ConcurrentBag<TodoInfo> bag)
        {
            bool inBlock = false;
            int lineIndex = 0;
            foreach (var raw in File.ReadLines(fullPath, Encoding.UTF8))
            {
                lineIndex++;
                string line = raw;
                int idx = 0;
                while (idx < line.Length)
                {
                    if (!inBlock)
                    {
                        int sl = line.IndexOf("//", idx, StringComparison.Ordinal);
                        int bl = line.IndexOf("/*", idx, StringComparison.Ordinal);
                        if (sl < 0 && bl < 0)
                            break;

                        if (sl >= 0 && (bl < 0 || sl < bl))
                        {
                            string comment = line.Substring(sl + 2);
                            string content = ExtractTodoFromComment(comment);
                            if (!string.IsNullOrWhiteSpace(content))
                                bag.Add(new TodoInfo { scriptPath = assetPath, line = lineIndex, content = content.Trim() });
                            break;
                        }
                        else
                        {
                            int end = line.IndexOf("*/", bl + 2, StringComparison.Ordinal);
                            if (end >= 0)
                            {
                                string comment = line.Substring(bl + 2, end - (bl + 2));
                                string content = ExtractTodoFromComment(comment);
                                if (!string.IsNullOrWhiteSpace(content))
                                    bag.Add(new TodoInfo { scriptPath = assetPath, line = lineIndex, content = content.Trim() });
                                idx = end + 2;
                                continue;
                            }
                            else
                            {
                                string comment = line.Substring(bl + 2);
                                string content = ExtractTodoFromComment(comment);
                                if (!string.IsNullOrWhiteSpace(content))
                                    bag.Add(new TodoInfo { scriptPath = assetPath, line = lineIndex, content = content.Trim() });
                                inBlock = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        int end = line.IndexOf("*/", idx, StringComparison.Ordinal);
                        string segment = end >= 0 ? line.Substring(idx, end - idx) : line.Substring(idx);
                        string content = ExtractTodoFromComment(segment);
                        if (!string.IsNullOrWhiteSpace(content))
                            bag.Add(new TodoInfo { scriptPath = assetPath, line = lineIndex, content = content.Trim() });
                        if (end >= 0)
                        {
                            inBlock = false;
                            idx = end + 2;
                            continue;
                        }
                        break;
                    }
                }
            }
        }

        private static string ExtractTodoFromComment(string commentSegment)
        {
            if (string.IsNullOrEmpty(commentSegment))
                return null;

            var m = Regex.Match(commentSegment, @"\bTODO\b\s*[:：]?\s*(.+)$", RegexOptions.IgnoreCase);
            if (m.Success)
                return m.Groups[1].Value;
            return null;
        }

        private static string BuildMarkdown(List<TodoInfo> todos)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# TODO 列表");
            sb.AppendLine();
            sb.AppendLine("注意：当完成某个 TODO 时，请将对应脚本中的 TODO 注释删除。");
            sb.AppendLine();
            sb.AppendLine($"- 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- 条目数: {todos.Count}");
            sb.AppendLine();
            sb.AppendLine("| 脚本路径 | 行号 | 内容 |");
            sb.AppendLine("| --- | --- | --- |");
            for (int i = 0; i < todos.Count; i++)
            {
                var t = todos[i];
                sb.Append('|');
                sb.Append(EscapeMarkdownCell(t.scriptPath));
                sb.Append('|');
                sb.Append(t.line.ToString());
                sb.Append('|');
                sb.Append(EscapeMarkdownCell(t.content));
                sb.AppendLine("|");
            }
            return sb.ToString();
        }

        private static string EscapeMarkdownCell(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            value = value.Replace("\r\n", "\n");
            value = value.Replace("\n", "<br/>");
            value = value.Replace("|", "\\|");
            return value;
        }

        private static void WriteMarkdown(string markdown, string outputPath)
        {
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(outputPath, markdown ?? string.Empty, new UTF8Encoding(false));
        }

        private struct TodoInfo
        {
            public string scriptPath;
            public int line;
            public string content;
        }
    }
}
