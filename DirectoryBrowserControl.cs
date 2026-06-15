// File: DirectoryBrowserControl.cs
// 自定义目录浏览器控件 - 每个格子独立使用

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace DirectoryGridBrowser
{
    public class DirectoryBrowserControl : UserControl
    {
        private const string DragSourceDirFormat = "DirectoryGridBrowser.SourceDir";

        private TextBox txtPath;
        private Button btnUp;
        private Button btnRefresh;
        private Button btnOpen;
        private Button btnBrowse;
        private ListView listViewFiles;
        private string currentDirectory = string.Empty;

        public string CurrentDirectory => currentDirectory;

        public event EventHandler? DirectoryChanged;

        public DirectoryBrowserControl(string? initialDirectory = null)
        {
            InitializeControls();
            string path = initialDirectory ?? string.Empty;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            SetCurrentDirectory(path);
        }

        private void InitializeControls()
        {
            // 顶部面板 (放置地址栏和按钮)
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(3)
            };

            txtPath = new TextBox
            {
                Location = new Point(5, 5),
                Width = topPanel.Width - 230,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F)
            };
            txtPath.KeyDown += TxtPath_KeyDown;

            btnUp = new Button
            {
                Text = "向上",
                Location = new Point(txtPath.Right + 5, 5),
                Width = 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnUp.Click += BtnUp_Click;

            btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(btnUp.Right + 5, 5),
                Width = 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnOpen = new Button
            {
                Text = "打开",
                Location = new Point(btnRefresh.Right + 5, 5),
                Width = 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnOpen.Click += BtnOpen_Click;

            btnBrowse = new Button
            {
                Text = "浏览",
                Location = new Point(btnOpen.Right + 5, 5),
                Width = 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnBrowse.Click += BtnBrowse_Click;

            topPanel.Controls.AddRange(new Control[] { txtPath, btnUp, btnRefresh, btnOpen, btnBrowse });

            // 文件列表视图
            listViewFiles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = true,
                Sorting = SortOrder.Ascending,
                LabelEdit = true
            };
            listViewFiles.Columns.Add("名称", 200);
            listViewFiles.Columns.Add("修改日期", 140);
            listViewFiles.Columns.Add("类型", 100);
            listViewFiles.Columns.Add("大小", 100);
            listViewFiles.DoubleClick += ListViewFiles_DoubleClick;
            listViewFiles.ColumnClick += ListViewFiles_ColumnClick;
            listViewFiles.KeyDown += ListViewFiles_KeyDown;
            listViewFiles.AfterLabelEdit += ListViewFiles_AfterLabelEdit;
            listViewFiles.AllowDrop = true;
            listViewFiles.ItemDrag += ListViewFiles_ItemDrag;
            listViewFiles.DragEnter += ListViewFiles_DragEnter;
            listViewFiles.DragOver += ListViewFiles_DragOver;
            listViewFiles.DragDrop += ListViewFiles_DragDrop;

            var contextMenu = new ContextMenuStrip();
            var menuCopy = new ToolStripMenuItem("复制(&C)");
            menuCopy.Click += (s, e) => CopySelectedItems();
            var menuPaste = new ToolStripMenuItem("粘贴(&V)");
            menuPaste.Click += (s, e) => PasteFromClipboard();
            var menuDelete = new ToolStripMenuItem("删除(&D)");
            menuDelete.Click += (s, e) => DeleteSelectedItems();
            var menuRename = new ToolStripMenuItem("重命名(&R)");
            menuRename.Click += (s, e) => RenameSelectedItem();
            contextMenu.Items.Add(menuCopy);
            contextMenu.Items.Add(menuPaste);
            contextMenu.Items.Add(menuRename);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuDelete);
            contextMenu.Opening += (s, e) =>
            {
                menuCopy.Enabled = listViewFiles.SelectedItems.Count > 0;
                menuPaste.Enabled = Clipboard.ContainsFileDropList();
                menuRename.Enabled = listViewFiles.SelectedItems.Count == 1;
                menuDelete.Enabled = listViewFiles.SelectedItems.Count > 0;
            };
            listViewFiles.ContextMenuStrip = contextMenu;

            Controls.Add(listViewFiles);
            Controls.Add(topPanel);

            // 调整地址栏宽度响应窗体大小变化
            this.Resize += (s, e) =>
            {
                int btnTotalWidth = btnUp.Width + btnRefresh.Width + btnOpen.Width + btnBrowse.Width + 25;
                txtPath.Width = topPanel.ClientSize.Width - btnTotalWidth - 20;
                if (txtPath.Width < 100) txtPath.Width = 100;
            };
        }

        private void TxtPath_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string newPath = txtPath.Text.Trim();
                if (Directory.Exists(newPath))
                    SetCurrentDirectory(newPath);
                else
                    MessageBox.Show($"目录不存在: {newPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUp_Click(object? sender, EventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(currentDirectory);
                if (di.Parent != null)
                {
                    SetCurrentDirectory(di.Parent.FullName);
                }
                else
                {
                    // 已经是根目录，无法向上
                    MessageBox.Show("已经是根目录，无法向上。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"向上失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentDirectory) && Directory.Exists(currentDirectory))
                RefreshAllGridsShowingDirectory(currentDirectory);
            else
                MessageBox.Show("当前目录无效，请重新选择。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentDirectory) || !Directory.Exists(currentDirectory))
            {
                MessageBox.Show("当前目录无效，无法打开。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(currentDirectory) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开目录: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "选择要浏览的目录";
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SetCurrentDirectory(fbd.SelectedPath);
                }
            }
        }

        private void ListViewFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedItems();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteFromClipboard();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedItems();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                RenameSelectedItem();
                e.Handled = true;
            }
        }

        private void RenameSelectedItem()
        {
            if (listViewFiles.SelectedItems.Count != 1)
            {
                if (listViewFiles.SelectedItems.Count > 1)
                    MessageBox.Show("一次只能重命名一个项目。", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            listViewFiles.SelectedItems[0].BeginEdit();
        }

        private void ListViewFiles_AfterLabelEdit(object? sender, LabelEditEventArgs e)
        {
            if (e.Label == null)
            {
                e.CancelEdit = true;
                return;
            }

            string newName = e.Label.Trim();
            ListViewItem item = listViewFiles.Items[e.Item];
            string oldName = item.Text;

            if (string.IsNullOrEmpty(newName) || string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase))
            {
                e.CancelEdit = true;
                return;
            }

            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("名称包含非法字符。", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.CancelEdit = true;
                return;
            }

            if (item.Tag is not string oldPath)
            {
                e.CancelEdit = true;
                return;
            }

            string newPath = Path.Combine(currentDirectory, newName);
            if (File.Exists(newPath) || Directory.Exists(newPath))
            {
                MessageBox.Show($"\"{newName}\" 已存在。", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.CancelEdit = true;
                return;
            }

            try
            {
                if (Directory.Exists(oldPath))
                    Directory.Move(oldPath, newPath);
                else if (File.Exists(oldPath))
                    File.Move(oldPath, newPath);
                else
                {
                    MessageBox.Show("源文件或文件夹不存在。", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.CancelEdit = true;
                    return;
                }

                RefreshAllGridsShowingDirectory(currentDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.CancelEdit = true;
            }
        }

        private void CopySelectedItems()
        {
            var paths = CollectSelectedPaths();
            if (paths.Count == 0)
                return;

            Clipboard.SetFileDropList(ToStringCollection(paths));
        }

        private void PasteFromClipboard()
        {
            if (!Clipboard.ContainsFileDropList())
                return;

            if (!IsCurrentDirectoryValid())
                return;

            var result = CopyPathsToDirectory(Clipboard.GetFileDropList(), currentDirectory, promptOnOverwrite: true);
            ShowTransferResult("粘贴", result);
            if (result.SuccessCount > 0)
                RefreshAllGridsShowingDirectory(currentDirectory);
        }

        private void ListViewFiles_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            var paths = CollectSelectedPaths();
            if (paths.Count == 0)
                return;

            var data = new DataObject();
            data.SetFileDropList(ToStringCollection(paths));
            data.SetData(DragSourceDirFormat, currentDirectory);
            listViewFiles.DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move);
        }

        private void ListViewFiles_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = GetDragDropEffect(e);
        }

        private void ListViewFiles_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = GetDragDropEffect(e);
        }

        private void ListViewFiles_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return;

            string? targetDir = GetDropTargetDirectory(e);
            if (string.IsNullOrEmpty(targetDir))
                return;

            string[] sources = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            string? sourceDir = e.Data.GetData(DragSourceDirFormat) as string;
            bool isInternal = sourceDir != null;
            bool useCopy = ShouldCopyOnDrop(e, isInternal);

            TransferResult result = useCopy
                ? CopyPathsToDirectory(sources, targetDir)
                : MovePathsToDirectory(sources, targetDir);

            ShowTransferResult(useCopy ? "复制" : "移动", result);

            if (result.SuccessCount > 0)
            {
                RefreshAllGridsShowingDirectory(targetDir);
                if (isInternal && sourceDir != null
                    && !IsSameDirectory(sourceDir, targetDir))
                {
                    RefreshAllGridsShowingDirectory(sourceDir);
                }
            }
        }

        private DragDropEffects GetDragDropEffect(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return DragDropEffects.None;

            string? targetDir = GetDropTargetDirectory(e);
            if (string.IsNullOrEmpty(targetDir))
                return DragDropEffects.None;

            string[]? sources = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (sources == null || sources.Length == 0)
                return DragDropEffects.None;

            string? sourceDir = e.Data.GetData(DragSourceDirFormat) as string;
            bool isInternal = sourceDir != null;

            if (isInternal && string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase))
                return DragDropEffects.None;

            foreach (string src in sources)
            {
                if (IsInvalidDropTarget(src, targetDir))
                    return DragDropEffects.None;
            }

            return ShouldCopyOnDrop(e, isInternal) ? DragDropEffects.Copy : DragDropEffects.Move;
        }

        private static bool ShouldCopyOnDrop(DragEventArgs e, bool isInternal)
        {
            bool ctrl = (e.KeyState & 8) != 0;
            bool shift = (e.KeyState & 4) != 0;
            if (ctrl) return true;
            if (shift) return false;
            return !isInternal;
        }

        private string? GetDropTargetDirectory(DragEventArgs e)
        {
            if (!IsCurrentDirectoryValid())
                return null;

            Point clientPoint = listViewFiles.PointToClient(new Point(e.X, e.Y));
            ListViewItem? item = listViewFiles.GetItemAt(clientPoint.X, clientPoint.Y);
            if (item?.Tag is string itemPath && Directory.Exists(itemPath))
                return itemPath;

            return currentDirectory;
        }

        private System.Collections.Generic.List<string> CollectSelectedPaths()
        {
            var paths = new System.Collections.Generic.List<string>();
            foreach (ListViewItem item in listViewFiles.SelectedItems)
            {
                if (item.Tag is string path && (File.Exists(path) || Directory.Exists(path)))
                    paths.Add(path);
            }
            return paths;
        }

        private static StringCollection ToStringCollection(System.Collections.Generic.IEnumerable<string> paths)
        {
            var collection = new StringCollection();
            foreach (string path in paths)
                collection.Add(path);
            return collection;
        }

        private bool IsCurrentDirectoryValid()
        {
            if (!string.IsNullOrEmpty(currentDirectory) && Directory.Exists(currentDirectory))
                return true;

            MessageBox.Show("当前目录无效，无法操作。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        private static bool IsInvalidDropTarget(string sourcePath, string targetDir)
        {
            string fullSource = Path.GetFullPath(sourcePath);
            string fullTarget = Path.GetFullPath(targetDir);

            if (string.Equals(fullSource, fullTarget, StringComparison.OrdinalIgnoreCase))
                return true;

            if (Directory.Exists(fullSource) && IsSubPathOf(fullSource, fullTarget))
                return true;

            return false;
        }

        private static bool IsSubPathOf(string parentPath, string childPath)
        {
            string parent = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string child = Path.GetFullPath(childPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
        }

        private struct TransferResult
        {
            public int SuccessCount;
            public System.Collections.Generic.List<string> Errors;
        }

        private static TransferResult CopyPathsToDirectory(System.Collections.IEnumerable sourcePaths, string destDir, bool promptOnOverwrite = false)
        {
            var result = new TransferResult { Errors = new System.Collections.Generic.List<string>() };

            foreach (string srcPath in sourcePaths)
            {
                try
                {
                    string name = Path.GetFileName(srcPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    string destPath = Path.Combine(destDir, name);

                    if (IsInvalidDropTarget(srcPath, destPath))
                    {
                        result.Errors.Add($"{name}: 无效的目标位置");
                        continue;
                    }

                    if (Directory.Exists(srcPath))
                    {
                        if (Directory.Exists(destPath) || File.Exists(destPath))
                        {
                            if (!ResolveExistingDestination(name, destPath, promptOnOverwrite, result.Errors))
                                continue;
                        }
                        CopyDirectory(srcPath, destPath);
                    }
                    else if (File.Exists(srcPath))
                    {
                        if (File.Exists(destPath) || Directory.Exists(destPath))
                        {
                            if (!ResolveExistingDestination(name, destPath, promptOnOverwrite, result.Errors))
                                continue;
                        }
                        File.Copy(srcPath, destPath);
                    }
                    else
                    {
                        result.Errors.Add($"{name}: 源不存在");
                        continue;
                    }
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{Path.GetFileName(srcPath)}: {ex.Message}");
                }
            }

            return result;
        }

        private static bool ResolveExistingDestination(
            string name,
            string destPath,
            bool promptOnOverwrite,
            System.Collections.Generic.List<string> errors)
        {
            if (!promptOnOverwrite)
            {
                errors.Add($"{name}: 目标已存在");
                return false;
            }

            string itemType = Directory.Exists(destPath) ? "文件夹" : "文件";
            var answer = MessageBox.Show(
                $"目标位置已存在{itemType} \"{name}\"，是否覆盖？",
                "粘贴",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return false;

            if (Directory.Exists(destPath))
                Directory.Delete(destPath, true);
            else if (File.Exists(destPath))
                File.Delete(destPath);
            return true;
        }

        private static TransferResult MovePathsToDirectory(System.Collections.IEnumerable sourcePaths, string destDir)
        {
            var result = new TransferResult { Errors = new System.Collections.Generic.List<string>() };

            foreach (string srcPath in sourcePaths)
            {
                try
                {
                    string name = Path.GetFileName(srcPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    string destPath = Path.Combine(destDir, name);

                    if (IsInvalidDropTarget(srcPath, destPath))
                    {
                        result.Errors.Add($"{name}: 无效的目标位置");
                        continue;
                    }

                    if (Directory.Exists(destPath) || File.Exists(destPath))
                    {
                        result.Errors.Add($"{name}: 目标已存在");
                        continue;
                    }

                    if (Directory.Exists(srcPath))
                        Directory.Move(srcPath, destPath);
                    else if (File.Exists(srcPath))
                        File.Move(srcPath, destPath);
                    else
                    {
                        result.Errors.Add($"{name}: 源不存在");
                        continue;
                    }
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{Path.GetFileName(srcPath)}: {ex.Message}");
                }
            }

            return result;
        }

        private static void ShowTransferResult(string action, TransferResult result)
        {
            if (result.Errors.Count == 0)
                return;

            string msg = result.SuccessCount > 0
                ? $"{action}成功 {result.SuccessCount} 项，以下失败：\n"
                : $"{action}失败：\n";
            msg += string.Join("\n", result.Errors);
            MessageBox.Show(msg, $"{action}结果", MessageBoxButtons.OK,
                result.SuccessCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }

        private void RefreshAllGridsShowingDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return;

            Control? root = FindForm();
            if (root == null)
                return;

            foreach (Control control in root.Controls)
                RefreshBrowserInControl(control, directory);
        }

        private static void RefreshBrowserInControl(Control control, string directory)
        {
            if (control is DirectoryBrowserControl browser
                && IsSameDirectory(browser.currentDirectory, directory))
            {
                browser.PopulateFileList(directory);
            }

            foreach (Control child in control.Controls)
                RefreshBrowserInControl(child, directory);
        }

        private static bool IsSameDirectory(string pathA, string pathB)
        {
            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
                return false;

            try
            {
                string fullA = Path.GetFullPath(pathA).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullB = Path.GetFullPath(pathB).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(fullA, fullB, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(pathA, pathB, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
            foreach (string subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }

        private void DeleteSelectedItems()
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;

            var paths = new System.Collections.Generic.List<string>();
            foreach (ListViewItem item in listViewFiles.SelectedItems)
            {
                if (item.Tag is string path && (File.Exists(path) || Directory.Exists(path)))
                    paths.Add(path);
            }

            if (paths.Count == 0)
                return;

            string prompt = paths.Count == 1
                ? $"确定要永久删除 \"{Path.GetFileName(paths[0])}\" 吗？\n此操作无法撤销。"
                : $"确定要永久删除选中的 {paths.Count} 项吗？\n此操作无法撤销。";

            if (MessageBox.Show(prompt, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int successCount = 0;
            var errors = new System.Collections.Generic.List<string>();

            foreach (string path in paths)
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, recursive: true);
                    else if (File.Exists(path))
                        File.Delete(path);
                    else
                    {
                        errors.Add($"{Path.GetFileName(path)}: 不存在");
                        continue;
                    }
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                }
            }

            if (successCount > 0)
                RefreshAllGridsShowingDirectory(currentDirectory);

            if (errors.Count > 0)
            {
                string msg = successCount > 0 ? $"已删除 {successCount} 项，以下失败：\n" : "删除失败：\n";
                msg += string.Join("\n", errors);
                MessageBox.Show(msg, "删除结果", MessageBoxButtons.OK,
                    successCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
            }
        }

        private void ListViewFiles_DoubleClick(object? sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0) return;
            ListViewItem item = listViewFiles.SelectedItems[0];
            string itemName = item.Text;
            string fullPath = Path.Combine(currentDirectory, itemName);

            if (Directory.Exists(fullPath))
            {
                // 进入子目录
                SetCurrentDirectory(fullPath);
            }
            else if (File.Exists(fullPath))
            {
                try
                {
                    // 用默认程序打开文件
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetCurrentDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            currentDirectory = directory;
            txtPath.Text = currentDirectory;
            PopulateFileList(currentDirectory);
            DirectoryChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateFileList(string directory)
        {
            // 使用等待光标，防止界面卡顿时用户操作
            Cursor currentCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            listViewFiles.BeginUpdate();
            listViewFiles.Items.Clear();

            try
            {
                // 获取目录列表
                string[] directories = Directory.GetDirectories(directory);
                // 获取文件列表
                string[] files = Directory.GetFiles(directory);

                // 添加目录项 (优先显示)
                foreach (string dir in directories)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir);
                    ListViewItem item = new ListViewItem(dirInfo.Name);
                    item.Tag = dir; // 存储完整路径备用
                    // 修改日期
                    DateTime lastWrite;
                    try { lastWrite = dirInfo.LastWriteTime; }
                    catch { lastWrite = DateTime.MinValue; }
                    item.SubItems.Add(lastWrite != DateTime.MinValue ? lastWrite.ToString("yyyy-MM-dd HH:mm:ss") : "");
                    // 类型
                    item.SubItems.Add("文件夹");
                    // 大小 (文件夹不显示大小)
                    item.SubItems.Add("");
                    listViewFiles.Items.Add(item);
                }

                // 添加文件项
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    ListViewItem item = new ListViewItem(fileInfo.Name);
                    item.Tag = file;
                    // 修改日期
                    DateTime lastWrite;
                    try { lastWrite = fileInfo.LastWriteTime; }
                    catch { lastWrite = DateTime.MinValue; }
                    item.SubItems.Add(lastWrite != DateTime.MinValue ? lastWrite.ToString("yyyy-MM-dd HH:mm:ss") : "");
                    // 类型 (简单获取扩展名描述)
                    string ext = Path.GetExtension(file).ToLower();
                    string fileType = ext switch
                    {
                        ".txt" => "文本文件",
                        ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" => "图像文件",
                        ".exe" => "应用程序",
                        ".zip" or ".rar" or ".7z" => "压缩文件",
                        ".pdf" => "PDF文档",
                        ".doc" or ".docx" => "Word文档",
                        ".xls" or ".xlsx" => "Excel表格",
                        ".mp3" or ".wav" => "音频文件",
                        ".mp4" or ".avi" => "视频文件",
                        _ => string.IsNullOrEmpty(ext) ? "文件" : ext.TrimStart('.') + " 文件"
                    };
                    item.SubItems.Add(fileType);
                    // 大小 (转换为KB或MB)
                    string sizeText = "";
                    try
                    {
                        long length = fileInfo.Length;
                        if (length < 1024)
                            sizeText = $"{length} B";
                        else if (length < 1024 * 1024)
                            sizeText = $"{length / 1024.0:F1} KB";
                        else
                            sizeText = $"{length / (1024.0 * 1024.0):F1} MB";
                    }
                    catch { sizeText = ""; }
                    item.SubItems.Add(sizeText);
                    listViewFiles.Items.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"无法访问目录: {directory}\n权限不足。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载目录内容失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listViewFiles.EndUpdate();
                Cursor.Current = currentCursor;
            }
        }

        // 简单的列排序支持 (按点击列排序)
        private int lastSortColumn = -1;
        private bool sortAscending = true;
        private void ListViewFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (e.Column == lastSortColumn)
                sortAscending = !sortAscending;
            else
                sortAscending = true;

            lastSortColumn = e.Column;
            listViewFiles.ListViewItemSorter = new ListViewItemComparer(e.Column, sortAscending);
        }

        // 自定义比较器类，实现对ListView各列的排序
        private class ListViewItemComparer : System.Collections.IComparer
        {
            private int col;
            private bool ascending;
            public ListViewItemComparer(int column, bool ascending)
            {
                this.col = column;
                this.ascending = ascending;
            }

            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem itemX || y is not ListViewItem itemY)
                    return 0;

                string textX = itemX.SubItems[col].Text;
                string textY = itemY.SubItems[col].Text;

                // 根据列类型尝试比较数字或日期
                int result = 0;
                // 针对大小列或日期列做一些特殊处理
                if (col == 3) // 大小列 (可能包含 "KB", "MB", "B")
                {
                    double valX = ParseSizeToBytes(textX);
                    double valY = ParseSizeToBytes(textY);
                    result = valX.CompareTo(valY);
                }
                else if (col == 1) // 修改日期列
                {
                    if (DateTime.TryParse(textX, out DateTime dtX) && DateTime.TryParse(textY, out DateTime dtY))
                        result = dtX.CompareTo(dtY);
                    else
                        result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
                }

                return ascending ? result : -result;
            }

            private double ParseSizeToBytes(string sizeText)
            {
                if (string.IsNullOrWhiteSpace(sizeText)) return 0;
                try
                {
                    if (sizeText.EndsWith("KB"))
                        return double.Parse(sizeText.Replace("KB", "").Trim()) * 1024;
                    if (sizeText.EndsWith("MB"))
                        return double.Parse(sizeText.Replace("MB", "").Trim()) * 1024 * 1024;
                    if (sizeText.EndsWith("B"))
                        return double.Parse(sizeText.Replace("B", "").Trim());
                    return 0;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}