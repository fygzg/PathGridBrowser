// File: MainForm.cs
// 主窗体 - 管理多个格子（目录浏览器控件）

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DirectoryGridBrowser
{
    public class MainForm : Form
    {
        private const string DefaultWindowTitle = "多格目录浏览器 - 每个格子独立浏览目录";

        private GridResizeHost gridHost;
        private Button btnAddGrid;
        private Button btnRemoveGrid;
        private Button btnEditTitle;
        private Bitmap? _iconBitmap;
        private TableLayoutPanel tableLayout => gridHost.TableLayout;

        public MainForm()
        {
            InitializeComponent();
            FormClosing += MainForm_FormClosing;
            RestoreSession();
        }

        private void InitializeComponent()
        {
            this.Text = DefaultWindowTitle;
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            SetApplicationIcon();

            gridHost = new GridResizeHost
            {
                Dock = DockStyle.Fill
            };
            gridHost.LayoutSizesChanged += (_, _) => SaveSession();

            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            btnRemoveGrid = new Button
            {
                Text = "− 减少格子",
                Dock = DockStyle.Left,
                Width = 150,
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnRemoveGrid.Click += BtnRemoveGrid_Click;

            btnAddGrid = new Button
            {
                Text = "+ 添加新格子",
                Dock = DockStyle.Right,
                Width = 150,
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnAddGrid.Click += BtnAddGrid_Click;

            btnEditTitle = new Button
            {
                Text = "修改标题",
                Dock = DockStyle.Fill,
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnEditTitle.Click += BtnEditTitle_Click;

            bottomPanel.Controls.Add(btnEditTitle);
            bottomPanel.Controls.Add(btnRemoveGrid);
            bottomPanel.Controls.Add(btnAddGrid);

            this.Controls.Add(gridHost);
            this.Controls.Add(bottomPanel);
        }

        private void SetApplicationIcon()
        {
            using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("DirectoryGridBrowser.icon.png");
            if (stream == null)
                return;

            _iconBitmap = new Bitmap(stream);
            Icon = Icon.FromHandle(_iconBitmap.GetHicon());
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSession();
        }

        private void SaveSession()
        {
            var browsers = GetBrowsersInOrder();
            var session = new AppSession
            {
                GridCount = browsers.Count,
                Directories = browsers.ConvertAll(b => b.CurrentDirectory),
                ColumnWidths = gridHost.GetColumnWidths(),
                RowHeights = gridHost.GetRowHeights(),
                WindowTitle = Text
            };
            SessionStorage.Save(session);
        }

        private void RestoreSession()
        {
            AppSession? session = SessionStorage.Load();
            if (session != null && session.GridCount > 0)
            {
                Text = string.IsNullOrWhiteSpace(session.WindowTitle) ? DefaultWindowTitle : session.WindowTitle;
                CreateGrids(session.GridCount, session.Directories, session.ColumnWidths, session.RowHeights);
                return;
            }

            CreateGrids(4, null, null, null);
        }

        private List<DirectoryBrowserControl> GetBrowsersInOrder()
        {
            var list = new List<DirectoryBrowserControl>();
            for (int row = 0; row < tableLayout.RowCount; row++)
            {
                for (int col = 0; col < tableLayout.ColumnCount; col++)
                {
                    if (tableLayout.GetControlFromPosition(col, row) is DirectoryBrowserControl browser)
                        list.Add(browser);
                }
            }
            return list;
        }

        private void CreateGrids(int gridCount, IList<string>? directories, IList<float>? columnWidths, IList<float>? rowHeights)
        {
            gridCount = Math.Max(1, gridCount);
            int cols = (int)Math.Ceiling(Math.Sqrt(gridCount));
            int rows = (int)Math.Ceiling((double)gridCount / cols);

            tableLayout.Controls.Clear();
            gridHost.SetLayoutSizes(rows, cols, columnWidths, rowHeights);

            for (int index = 0; index < gridCount; index++)
            {
                string? dir = directories != null && index < directories.Count ? directories[index] : null;
                var browser = new DirectoryBrowserControl(dir);
                browser.Dock = DockStyle.Fill;
                browser.Margin = new Padding(2);
                browser.DirectoryChanged += (_, _) => SaveSession();
                int row = index / cols;
                int col = index % cols;
                tableLayout.Controls.Add(browser, col, row);
            }
        }

        private void BtnAddGrid_Click(object? sender, EventArgs e)
        {
            int totalCells = tableLayout.ColumnCount * tableLayout.RowCount;
            ResizeGridLayout(totalCells + 1);
            SaveSession();
        }

        private void BtnRemoveGrid_Click(object? sender, EventArgs e)
        {
            int totalCells = tableLayout.ColumnCount * tableLayout.RowCount;
            if (totalCells <= 1)
            {
                MessageBox.Show("至少保留一个格子。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var existingBrowsers = GetBrowsersInOrder();
            existingBrowsers[existingBrowsers.Count - 1].Dispose();

            ResizeGridLayout(totalCells - 1, totalCells - 1);
            SaveSession();
        }

        private void ResizeGridLayout(int targetCellCount, int? keepBrowserCount = null)
        {
            int keepCount = keepBrowserCount ?? targetCellCount;
            int newCols = (int)Math.Ceiling(Math.Sqrt(targetCellCount));
            int newRows = (int)Math.Ceiling((double)targetCellCount / newCols);
            RebuildTableLayout(newRows, newCols, keepCount);
        }

        private void RebuildTableLayout(int newRows, int newCols, int keepBrowserCount)
        {
            var existingBrowsers = GetBrowsersInOrder();

            tableLayout.SuspendLayout();
            tableLayout.Controls.Clear();

            bool sameShape = newRows == tableLayout.RowCount && newCols == tableLayout.ColumnCount;
            if (!sameShape)
                gridHost.SetLayoutSizes(newRows, newCols, null, null);
            else
                gridHost.SetLayoutSizes(newRows, newCols, gridHost.GetColumnWidths(), gridHost.GetRowHeights());

            int index = 0;
            for (int row = 0; row < newRows; row++)
            {
                for (int col = 0; col < newCols; col++)
                {
                    if (index < keepBrowserCount && index < existingBrowsers.Count)
                    {
                        var browser = existingBrowsers[index];
                        tableLayout.Controls.Add(browser, col, row);
                    }
                    else if (index < newRows * newCols)
                    {
                        var newBrowser = new DirectoryBrowserControl();
                        newBrowser.Dock = DockStyle.Fill;
                        newBrowser.Margin = new Padding(2);
                        newBrowser.DirectoryChanged += (_, _) => SaveSession();
                        tableLayout.Controls.Add(newBrowser, col, row);
                    }
                    index++;
                }
            }
            tableLayout.ResumeLayout();
            tableLayout.PerformLayout();
        }

        private void BtnEditTitle_Click(object? sender, EventArgs e)
        {
            string? newTitle = PromptForWindowTitle(Text);
            if (newTitle == null)
                return;

            Text = string.IsNullOrWhiteSpace(newTitle) ? DefaultWindowTitle : newTitle.Trim();
            SaveSession();
        }

        private string? PromptForWindowTitle(string currentTitle)
        {
            using var form = new Form
            {
                Text = "修改标题",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(420, 120)
            };

            var label = new Label
            {
                Text = "窗口标题：",
                Location = new Point(12, 16),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Text = currentTitle,
                Location = new Point(12, 40),
                Width = 396,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var btnOk = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(252, 78),
                Width = 75
            };

            var btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(333, 78),
                Width = 75
            };

            form.Controls.AddRange(new Control[] { label, textBox, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog(this) == DialogResult.OK ? textBox.Text : null;
        }
    }
}
