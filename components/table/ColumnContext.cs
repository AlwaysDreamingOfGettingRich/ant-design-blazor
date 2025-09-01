// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace AntDesign
{
    public class ColumnContext
    {
        public IList<IColumn> Columns { get; set; } = new List<IColumn>();

        public IList<IColumn> HeaderColumns { get; set; } = new List<IColumn>();

        private int[] ColIndexOccupied { get; set; }

        private ITable _table;

        public ColumnContext(ITable table)
        {
            _table = table;
        }

        public void AddColumn(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            // 直接根据Columns集合的实际顺序分配索引
            column.ColIndex = Columns.Count;
            Columns.Add(column);
        }

        public void AddHeaderColumn(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            var columnSpan = column.HeaderColSpan;
            if (column.RowSpan == 0) columnSpan = 0;

            // 多级表头：确保HeaderColumns的索引与Columns对应
            if (HeaderColumns.Count < Columns.Count)
            {
                // 找到对应的普通列
                var correspondingColumn = Columns[HeaderColumns.Count];
                column.ColIndex = correspondingColumn.ColIndex;
            }
            else
            {
                // 如果HeaderColumns比Columns多，使用最后一个索引
                column.ColIndex = Columns.Count > 0 ? Columns.Count - 1 : 0;
            }

            HeaderColumns.Add(column);
        }

        public void AddColGroup(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            // ColGroup的索引必须与Columns中的对应列一致
            if (Columns.Any())
            {
                var colIndex = Columns.Count - 1;
                if (colIndex >= 0 && colIndex < Columns.Count)
                {
                    column.ColIndex = Columns[colIndex].ColIndex;
                }
                else
                {
                    column.ColIndex = 0;
                }
            }
            else
            {
                column.ColIndex = 0;
            }

            if (_table.ScrollX != null && Columns.Any(x => x.Width == null))
            {
                var zeroWidthCols = Columns.Where(x => x.Width == null).ToArray();
                var totalWidth = string.Join(" + ", Columns.Where(x => x.Width != null).Select(x => (CssSizeLength)x.Width));
                if (string.IsNullOrEmpty(totalWidth))
                {
                    totalWidth = "0px";
                }
                foreach (var col in Columns.Where(x => x.Width == null))
                {
                    col.Width = $"calc(({(CssSizeLength)_table.ScrollX} - ({totalWidth}) ) / {zeroWidthCols.Length})";
                }
            }

            if (column.Width == null)
            {
                var col = Columns.FirstOrDefault(x => x.ColIndex == column.ColIndex);
                if (col != null)
                {
                    column.Width = col.Width;
                }
            }
        }

        public void AddRowColumn(IColumn column)
        {
            if (column == null)
            {
                return;
            }

            var columnSpan = column.ColSpan;
            if (column.RowSpan == 0) columnSpan = 0;

            // 确保行列的索引与Columns集合对应
            if (_table.AutoColIndexes)
            {
                if (Columns.Any())
                {
                    // 找到对应的普通列
                    var correspondingColumn = Columns.LastOrDefault();
                    if (correspondingColumn != null)
                    {
                        column.ColIndex = correspondingColumn.ColIndex;
                    }
                    else
                    {
                        column.ColIndex = 0;
                    }
                }
                else
                {
                    column.ColIndex = 0;
                }
            }
            else
            {
                // 手动设置ColIndex时，确保它在合理范围内
                if (column.ColIndex < 0 || column.ColIndex >= Columns.Count)
                {
                    column.ColIndex = Columns.Count > 0 ? Columns.Count - 1 : 0;
                }
            }
        }

        internal void HeaderColumnInitialized(IColumn column)
        {
            // 确保所有列的索引一致
            if (HeaderColumns.Count == Columns.Count)
            {
                // 同步HeaderColumns和Columns的索引
                for (int i = 0; i < Columns.Count; i++)
                {
                    if (i < HeaderColumns.Count)
                    {
                        HeaderColumns[i].ColIndex = Columns[i].ColIndex;
                    }
                }

                _table.OnColumnInitialized();
            }
        }
    }
}
