using Lin.Editor.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lin.Editor.Asset
{
    [CustomEditor(typeof(TextAsset))]
    public class TextAssetEditor : UnityEditor.Editor
    {
        #region - 创建TextAsset -

#if UNITY_6000_4_OR_NEWER
        private class OnCreateText : AssetCreationEndAction
        {
            public override void Action(EntityId instanceId, string pathName, string resourceFile)
#else
        private class OnCreateText : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
#endif
            {
                Object result = CreateScriptAssetFromTemplate(pathName);
                ProjectWindowUtil.ShowCreatedAsset(result);
            }

            internal static Object CreateScriptAssetFromTemplate(string pathName)
            {
                string fullPath = Path.GetFullPath(pathName);
                using (File.CreateText(fullPath)) { }
                AssetDatabase.ImportAsset(pathName);
                return AssetDatabase.LoadAssetAtPath(pathName, typeof(Object));
            }
        }

        [MenuItem("Assets/Create/TextAsset", false, 80)]
        private static void CreateText()
        {
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None,
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
#endif
                CreateInstance<OnCreateText>(),
                PathHelper.GetSelectedPathOrFallback() + "/New Text.txt",
                null,
                default
                );
        }

        #endregion

        #region - 批量编码转换 -

        [MenuItem("Lin/批量转换文件编码为UTF-8")]
        private static void BatchConvertToUTF8()
        {
            string dataPath = Application.dataPath;
            var files = FindAllTextFiles(dataPath);
            
            if (files.Count == 0)
            {
                UnityEditor.EditorUtility.DisplayDialog("提示", "未找到任何.cs或.txt文件", "确定");
                return;
            }

            if (!UnityEditor.EditorUtility.DisplayDialog("批量转换确认", 
                $"找到 {files.Count} 个文件需要检测转换：\n" +
                $"• .cs 文件: {CountByExtension(files, ".cs")}\n" +
                $"• .txt 文件: {CountByExtension(files, ".txt")}\n\n" +
                "是否继续进行批量转换？", "确定", "取消"))
            {
                return;
            }

            BatchConvertFiles(files);
        }

        /// <summary>
        /// 统计列表中以指定扩展名结尾的文件数
        /// </summary>
        private static int CountByExtension(List<string> files, string extension)
        {
            int count = 0;
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 递归查找所有.cs和.txt文件
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <returns>文件路径列表</returns>
        // 公开工具方法，供导入器等其他编辑器脚本使用
        public static List<string> FindAllTextFiles(string rootPath)
        {
            List<string> files = new List<string>();
            
            try
            {
                // 递归搜索所有.cs和.txt文件
                SearchDirectory(rootPath, files);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"搜索文件时出错: {ex.Message}");
            }

            return files;
        }

        /// <summary>
        /// 递归搜索目录
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <param name="files">文件列表</param>
        public static void SearchDirectory(string directory, List<string> files)
        {
            try
            {
                // 搜索当前目录下的.cs和.txt文件
                string[] csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
                string[] txtFiles = Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly);
                
                files.AddRange(csFiles);
                files.AddRange(txtFiles);

                // 递归搜索子目录
                string[] subDirectories = Directory.GetDirectories(directory);
                foreach (string subDir in subDirectories)
                {
                    // 跳过一些不需要处理的目录
                    string dirName = Path.GetFileName(subDir);
                    if (dirName.StartsWith(".") || 
                        dirName.Equals("Library", System.StringComparison.OrdinalIgnoreCase) ||
                        dirName.Equals("Temp", System.StringComparison.OrdinalIgnoreCase) ||
                        dirName.Equals("Logs", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    SearchDirectory(subDir, files);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 跳过无权限访问的目录
                Debug.LogWarning($"跳过无权限访问的目录: {directory}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"搜索目录 {directory} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量转换文件编码
        /// </summary>
        /// <param name="files">文件列表</param>
        public static void BatchConvertFiles(List<string> files)
        {
            int totalFiles = files.Count;
            int convertedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            for (int i = 0; i < totalFiles; i++)
            {
                string file = files[i];
                string relativePath = file.Replace(Application.dataPath, "Assets");
                
                // 显示进度条
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("批量转换文件编码", 
                    $"正在处理: {Path.GetFileName(file)} ({i + 1}/{totalFiles})", 
                    (float)i / totalFiles))
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                    UnityEditor.EditorUtility.DisplayDialog("操作取消", $"已处理 {i} 个文件，操作被用户取消", "确定");
                    return;
                }

                try
                {
                    var encoding = DetectFileEncodingStatic(file);
                    
                    if (encoding.Equals(Encoding.UTF8))
                    {
                        skippedCount++;
                        //Debug.Log($"跳过已是UTF-8编码的文件: {relativePath}");
                    }
                    else
                    {
                        if (ConvertToUTF8Static(file))
                        {
                            convertedCount++;
                            Debug.Log($"成功转换文件: {relativePath} ({GetEncodingDisplayNameStatic(encoding)} -> UTF-8)");
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    errorCount++;
                    Debug.LogError($"处理文件 {relativePath} 时出错: {ex.Message}");
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            // 显示结果统计
            string resultMessage = $"批量转换完成！\n\n" +
                                 $"总文件数: {totalFiles}\n" +
                                 $"成功转换: {convertedCount}\n" +
                                 $"跳过(已是UTF-8): {skippedCount}\n" +
                                 $"转换失败: {errorCount}";

            UnityEditor.EditorUtility.DisplayDialog("转换完成", resultMessage, "确定");
        }

        /// <summary>
        /// 静态版本的编码检测方法
        /// </summary>
        public static Encoding DetectFileEncodingStatic(string filePath)
        {
            if (!File.Exists(filePath))
                return Encoding.UTF8;

            byte[] buffer = new byte[4];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length >= 4)
                {
                    fs.Read(buffer, 0, 4);
                }
                else if (fs.Length > 0)
                {
                    fs.Read(buffer, 0, (int)fs.Length);
                }
            }

            // 检测BOM
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;
            if (buffer.Length >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                return Encoding.UTF32;
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
                return Encoding.Unicode;
            if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            // 读取更多字节进行编码检测
            byte[] allBytes = File.ReadAllBytes(filePath);
            
            // 检测是否为有效的UTF-8（无BOM）
            if (IsValidUTF8(allBytes))
            {
                return Encoding.UTF8;
            }

            // 尝试其他编码
            try
            {
                // 尝试GB2312/GBK编码
                string content = Encoding.GetEncoding("GB2312").GetString(allBytes);
                if (!string.IsNullOrEmpty(content) && !content.Contains('\uFFFD'))
                {
                    return Encoding.GetEncoding("GB2312");
                }
            }
            catch { }

            // 默认返回系统默认编码（通常是ANSI）
            return Encoding.Default;
        }

        /// <summary>
        /// 检测字节数组是否为有效的UTF-8编码
        /// </summary>
        public static bool IsValidUTF8(byte[] bytes)
        {
            try
            {
                var decoder = Encoding.UTF8.GetDecoder();
                decoder.Fallback = DecoderFallback.ExceptionFallback;
                
                char[] chars = new char[decoder.GetCharCount(bytes, 0, bytes.Length)];
                decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
                
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 静态版本的UTF-8转换方法
        /// </summary>
        public static bool ConvertToUTF8Static(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"文件不存在: {filePath}");
                    return false;
                }

                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // 文件可以访问
                    }
                }
                catch (IOException)
                {
                    Debug.LogError($"文件被占用，无法转换: {filePath}");
                    return false;
                }

                Encoding currentEncoding = DetectFileEncodingStatic(filePath);
                
                if (currentEncoding.Equals(Encoding.UTF8))
                {
                    return true;
                }

                string content = File.ReadAllText(filePath, currentEncoding);
                File.WriteAllText(filePath, content, new UTF8Encoding(true));
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"编码转换失败 {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 静态版本的编码显示名称方法
        /// </summary>
        public static string GetEncodingDisplayNameStatic(Encoding encoding)
        {
            if (encoding.Equals(Encoding.UTF8))
                return "UTF-8";
            if (encoding.Equals(Encoding.Unicode))
                return "UTF-16 LE";
            if (encoding.Equals(Encoding.BigEndianUnicode))
                return "UTF-16 BE";
            if (encoding.Equals(Encoding.UTF32))
                return "UTF-32";
            if (encoding.CodePage == 936)
                return "GB2312";
            
            return encoding.EncodingName;
        }

        #endregion

        #region - 文本转换 -

        /// <summary>
        /// 检测文件的编码格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>检测到的编码格式</returns>
        private Encoding DetectFileEncoding(string filePath)
        {
            if (!File.Exists(filePath))
                return Encoding.UTF8;

            byte[] buffer = new byte[4];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length >= 4)
                {
                    fs.Read(buffer, 0, 4);
                }
                else if (fs.Length > 0)
                {
                    fs.Read(buffer, 0, (int)fs.Length);
                }
            }

            // 检测BOM
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;
            if (buffer.Length >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                return Encoding.UTF32;
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
                return Encoding.Unicode;
            if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            // 尝试读取文件内容进行编码检测
            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                // 如果UTF-8读取没有问题，则认为是UTF-8
                return Encoding.UTF8;
            }
            catch
            {
                // 如果UTF-8读取失败，尝试其他编码
                try
                {
                    File.ReadAllText(filePath, Encoding.GetEncoding("GB2312"));
                    return Encoding.GetEncoding("GB2312");
                }
                catch
                {
                    return Encoding.Default;
                }
            }
        }

        [MenuItem("Assets/Json格式化")]
        private static void JsonFormatting()
        {
            // 对选中的 TextAsset 文件进行 Json 格式化：尝试解析为 JToken，再以缩进格式写回
            var assets = Selection.GetFiltered<TextAsset>(SelectionMode.Assets);
            if (assets == null || assets.Length == 0)
            {
                UnityEditor.EditorUtility.DisplayDialog("提示", "请在 Project 视图中选择至少一个 TextAsset", "确定");
                return;
            }

            if (!UnityEditor.EditorUtility.DisplayDialog("Json 格式化确认",
                $"将对 {assets.Length} 个 TextAsset 尝试进行 Json 格式化，是否继续？",
                "确定", "取消"))
            {
                return;
            }

            int total = assets.Length;
            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;

            for (int i = 0; i < total; i++)
            {
                var asset = assets[i];
                var path = AssetDatabase.GetAssetPath(asset);

                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Json 格式化",
                    $"正在处理: {System.IO.Path.GetFileName(path)} ({i + 1}/{total})",
                    (float)i / total))
                {
                    UnityEditor.EditorUtility.ClearProgressBar();
                    UnityEditor.EditorUtility.DisplayDialog("操作取消", $"已处理 {i} 个文件，操作被用户取消", "确定");
                    return;
                }

                try
                {
                    var encoding = DetectFileEncodingStatic(path);
                    var text = System.IO.File.ReadAllText(path, encoding);

                    // 空内容或纯空白视为跳过
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        skipCount++;
                        continue;
                    }

                    // 尝试解析为 JToken 以兼容对象或数组根
                    var token = JToken.Parse(text);

                    // 缩进格式输出，保留非 ASCII 字符便于人工查看
                    var formatted = token.ToString(Formatting.Indented);

                    // 写回原文件，保持原编码
                    System.IO.File.WriteAllText(path, formatted, encoding);

                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Json 格式化失败: {path} -> {ex.Message}");
                    failCount++;
                }
            }

            UnityEditor.EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            UnityEditor.EditorUtility.DisplayDialog("格式化完成",
                $"总数: {total}\n成功: {successCount}\n跳过: {skipCount}\n失败: {failCount}",
                "确定");
        }

        #endregion

        private enum EditorState
        {
            Display,
            Input
        }

        private EditorState state = EditorState.Display;
        private string input;
        private Encoding currentEncoding;

        private void OnEnable()
        {
            state = EditorState.Display;
            string path = AssetDatabase.GetAssetPath(target);
            
            try
            {
                currentEncoding = DetectFileEncoding(path);
                input = File.ReadAllText(path, currentEncoding);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取文件失败: {ex.Message}");
                currentEncoding = Encoding.UTF8;
                input = "文件读取失败，请检查文件编码格式";
            }
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();

            if (state == EditorState.Display)
                GUILayout.Label(input);
            else
                input = GUILayout.TextArea(input);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(state == EditorState.Input);
                if (GUILayout.Button("修改"))
                {
                    state = EditorState.Input;
                    string path = AssetDatabase.GetAssetPath(target);
                    input = File.ReadAllText(path, currentEncoding);
                }
                EditorGUI.EndDisabledGroup();
            }
            {
                EditorGUI.BeginDisabledGroup(state == EditorState.Display);
                if (GUILayout.Button("保存"))
                {
                    string path = AssetDatabase.GetAssetPath(target);
                    state = EditorState.Display;
                    File.WriteAllText(path, input, currentEncoding);
                    AssetDatabase.Refresh();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }
    }
}
