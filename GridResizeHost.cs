// File: GridResizeHost.cs
// 在格子之间提供可拖拽分隔条以调整大小

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DirectoryGridBrowser
{
    public class GridResizeHost : Panel
    {
        private const int SplitterThickness = 6;
        private const float MinCellPercent = 8f;

        private readonly TableLayoutPanel tableLayout;
        private readonly List<Panel> verticalSplitters = new();
        private readonly List<Panel> horizontalSplitters = new();

        private bool isDragging;
        private bool dragVertical;
        private int dragIndex;
        private int dragStartPos;
        private float dragStartLeading;
        private float dragStartTrailing;

        public TableLayoutPanel TableLayout => tableLayout;
        public event EventHandler? LayoutSizesChanged;

        public GridResizeHost()
        {
            tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.LightGray
            };
            Controls.Add(tableLayout);
            Resize += (_, _) => UpdateSplitterPositions();
        }

        public void SetLayoutSizes(int rows, int cols, IList<float>? columnWidths, IList<float>? rowHeights)
        {
            tableLayout.SuspendLayout();
            tableLayout.RowStyles.Clear();
            tableLayout.ColumnStyles.Clear();
            tableLayout.RowCount = rows;
            tableLayout.ColumnCount = cols;

            for (int i = 0; i < cols; i++)
            {
                float width = columnWidths != null && i < columnWidths.Count
                    ? columnWidths[i]
                    : 100f / cols;
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
            }

            for (int i = 0; i < rows; i++)
            {
                float height = rowHeights != null && i < rowHeights.Count
                    ? rowHeights[i]
                    : 100f / rows;
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, height));
            }

            tableLayout.ResumeLayout();
            RecreateSplitters();
        }

        public List<float> GetColumnWidths()
        {
            var list = new List<float>();
            foreach (ColumnStyle style in tableLayout.ColumnStyles)
                list.Add(style.Width);
            return list;
        }

        public List<float> GetRowHeights()
        {
            var list = new List<float>();
            foreach (RowStyle style in tableLayout.RowStyles)
                list.Add(style.Height);
            return list;
        }

        private void RecreateSplitters()
        {
            foreach (Panel splitter in verticalSplitters)
            {
                Controls.Remove(splitter);
                splitter.Dispose();
            }
            foreach (Panel splitter in horizontalSplitters)
            {
                Controls.Remove(splitter);
                splitter.Dispose();
            }
            verticalSplitters.Clear();
            horizontalSplitters.Clear();

            for (int i = 0; i < tableLayout.ColumnCount - 1; i++)
            {
                var splitter = CreateSplitterPanel(true, i);
                verticalSplitters.Add(splitter);
                Controls.Add(splitter);
                splitter.BringToFront();
            }

            for (int i = 0; i < tableLayout.RowCount - 1; i++)
            {
                var splitter = CreateSplitterPanel(false, i);
                horizontalSplitters.Add(splitter);
                Controls.Add(splitter);
                splitter.BringToFront();
            }

            UpdateSplitterPositions();
        }

        private Panel CreateSplitterPanel(bool vertical, int index)
        {
            var splitter = new Panel
            {
                BackColor = Color.FromArgb(200, 140, 140, 140),
                Cursor = vertical ? Cursors.SizeWE : Cursors.SizeNS
            };

            splitter.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left)
                    return;

                isDragging = true;
                dragVertical = vertical;
                dragIndex = index;
                dragStartPos = vertical ? Cursor.Position.X : Cursor.Position.Y;

                if (vertical)
                {
                    dragStartLeading = tableLayout.ColumnStyles[index].Width;
                    dragStartTrailing = tableLayout.ColumnStyles[index + 1].Width;
                }
                else
                {
                    dragStartLeading = tableLayout.RowStyles[index].Height;
                    dragStartTrailing = tableLayout.RowStyles[index + 1].Height;
                }

                splitter.Capture = true;
            };

            splitter.MouseMove += (_, e) =>
            {
                if (!isDragging || dragIndex != index)
                    return;

                int currentPos = dragVertical ? Cursor.Position.X : Cursor.Position.Y;
                int delta = currentPos - dragStartPos;
                float total = dragVertical ? tableLayout.ClientSize.Width : tableLayout.ClientSize.Height;
                if (total <= 0)
                    return;

                float totalPercent = dragVertical ? GetTotalColumnPercent() : GetTotalRowPercent();
                float deltaPercent = delta * totalPercent / total;

                float newLeading = dragStartLeading + deltaPercent;
                float newTrailing = dragStartTrailing - deltaPercent;
                if (newLeading < MinCellPercent || newTrailing < MinCellPercent)
                    return;

                if (dragVertical)
                {
                    tableLayout.ColumnStyles[dragIndex].Width = newLeading;
                    tableLayout.ColumnStyles[dragIndex + 1].Width = newTrailing;
                }
                else
                {
                    tableLayout.RowStyles[dragIndex].Height = newLeading;
                    tableLayout.RowStyles[dragIndex + 1].Height = newTrailing;
                }

                tableLayout.PerformLayout();
                UpdateSplitterPositions();
            };

            splitter.MouseUp += (_, e) =>
            {
                if (!isDragging || dragIndex != index)
                    return;

                isDragging = false;
                splitter.Capture = false;
                LayoutSizesChanged?.Invoke(this, EventArgs.Empty);
            };

            return splitter;
        }

        private void UpdateSplitterPositions()
        {
            if (tableLayout.ColumnCount == 0 || tableLayout.RowCount == 0)
                return;

            int x = tableLayout.Left;
            for (int col = 0; col < tableLayout.ColumnCount - 1; col++)
            {
                x += GetColumnPixelWidth(col);
                Panel splitter = verticalSplitters[col];
                splitter.SetBounds(x - SplitterThickness / 2, tableLayout.Top, SplitterThickness, tableLayout.Height);
            }

            int y = tableLayout.Top;
            for (int row = 0; row < tableLayout.RowCount - 1; row++)
            {
                y += GetRowPixelHeight(row);
                Panel splitter = horizontalSplitters[row];
                splitter.SetBounds(tableLayout.Left, y - SplitterThickness / 2, tableLayout.Width, SplitterThickness);
            }
        }

        private int GetColumnPixelWidth(int col)
        {
            float total = GetTotalColumnPercent();
            if (total <= 0)
                return 0;
            return (int)(tableLayout.ClientSize.Width * tableLayout.ColumnStyles[col].Width / total);
        }

        private int GetRowPixelHeight(int row)
        {
            float total = GetTotalRowPercent();
            if (total <= 0)
                return 0;
            return (int)(tableLayout.ClientSize.Height * tableLayout.RowStyles[row].Height / total);
        }

        private float GetTotalColumnPercent()
        {
            float total = 0;
            foreach (ColumnStyle style in tableLayout.ColumnStyles)
                total += style.Width;
            return total;
        }

        private float GetTotalRowPercent()
        {
            float total = 0;
            foreach (RowStyle style in tableLayout.RowStyles)
                total += style.Height;
            return total;
        }
    }
}
