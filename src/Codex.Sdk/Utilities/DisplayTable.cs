// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Codex.Utilities
{
    /// <summary>
    /// Displays a table with aligned text
    /// </summary>
    public sealed class DisplayTable<TEnum>
        where TEnum : unmanaged, System.Enum
    {
        private readonly int[] m_maxColumnLengths;
        private readonly List<string[]> m_rows = new List<string[]>();
        private string[] m_currentRow;
        private readonly string m_columnDelimeter;

        public bool LeftAlign { get; set; } = true;

        public DisplayTable(string columnDelimeter = " | ", bool defaultHeader = true)
        {
            m_maxColumnLengths = new int[EnumTraits<TEnum>.ValueCount];
            m_columnDelimeter = columnDelimeter;

            if (defaultHeader)
            {
                NextRow();
                foreach (var value in EnumTraits<TEnum>.EnumerateValues())
                {
                    Set(value, value.ToString());
                }
            }
        }

        public object this[TEnum column] { set => Set(column, value); }

        public void NextRow()
        {
            m_currentRow = new string[EnumTraits<TEnum>.ValueCount];
            m_rows.Add(m_currentRow);
        }

        public void Set(TEnum column, object value)
        {
            if (value == null)
            {
                return;
            }

            var stringValue = value.ToString();
            var columnIndex = EnumTraits<TEnum>.ToInteger(column);
            m_maxColumnLengths[columnIndex] = Math.Max(m_maxColumnLengths[columnIndex], stringValue.Length);
            m_currentRow[columnIndex] = stringValue;
        }

        public void Write(TextWriter writer)
        {
            StringBuilder sb = new StringBuilder();

            var buffer = new char[m_maxColumnLengths.Sum() + (m_columnDelimeter.Length * (EnumTraits<TEnum>.ValueCount - 1))];
            for (int r = 0; r < m_rows.Count; r++)
            {
                var row = m_rows[r];
                sb.Clear();

                for (int i = 0; i < row.Length; i++)
                {
                    var value = row[i] ?? string.Empty;
                    bool rightPad = i == 0 || r == 0 || LeftAlign;
                    void writePadding()
                    {
                        sb.Append(' ', m_maxColumnLengths[i] - value.Length);
                    }

                    if (!rightPad)
                    {
                        // Subsequent values pad to the left
                        writePadding();
                    }

                    sb.Append(value);

                    if (rightPad)
                    { 
                        // First value pads on right
                        writePadding();
                    }

                    if (i != (row.Length - 1))
                    {
                        sb.Append(m_columnDelimeter);
                    }
                }

                sb.CopyTo(0, buffer, 0, buffer.Length);
                writer.Write(buffer, 0, buffer.Length);

                if (row != m_currentRow)
                {
                    writer.WriteLine();
                }
            }
        }
    }
}