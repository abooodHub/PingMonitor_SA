using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class ServerRowComparer : IComparer
    {
        private readonly int column;
        private readonly bool descending;
        private readonly IDictionary<string, ServerMetrics> metrics;
        public ServerRowComparer(int column, bool descending, IDictionary<string, ServerMetrics> metrics)
        { this.column = column; this.descending = descending; this.metrics = metrics; }

        public int Compare(object x, object y)
        {
            ListViewItem a = (ListViewItem)x, b = (ListViewItem)y;
            int comparison;
            if (column >= 4)
            {
                double? av = GetValue(a), bv = GetValue(b);
                if (av.HasValue != bv.HasValue) return av.HasValue ? -1 : 1;
                comparison = av.HasValue ? av.Value.CompareTo(bv.Value) : 0;
            }
            else comparison = string.Compare(a.SubItems[column].Text, b.SubItems[column].Text, StringComparison.CurrentCultureIgnoreCase);
            if (comparison != 0) return descending ? -comparison : comparison;
            return string.Compare(((ServerRowTag)a.Tag).ServerId, ((ServerRowTag)b.Tag).ServerId, StringComparison.Ordinal);
        }

        private double? GetValue(ListViewItem item)
        {
            ServerMetrics value;
            if (!metrics.TryGetValue(((ServerRowTag)item.Tag).ServerId, out value) || !value.HasResult) return null;
            if (column == 6) return value.SampleCount == 0 ? (double?)null : value.LossPercent;
            if (!value.IsOnline) return null;
            return column == 4 ? value.PingMilliseconds : value.JitterMilliseconds;
        }
    }
}
