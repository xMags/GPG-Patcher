using System;

namespace GpgPatcher
{
    internal struct AccountGridMetrics
    {
        public int Columns { get; set; }

        public int CardWidth { get; set; }
    }

    internal static class AccountGridLayout
    {
        public const int GapPixels = 12;

        public static AccountGridMetrics Calculate(int clientWidth, float dpiScale, bool reserveScrollbar = true)
        {
            var scale = dpiScale <= 0f ? 1f : dpiScale;
            var scrollbarAllowance = reserveScrollbar ? (int)Math.Ceiling(22f * scale) : 0;
            var gap = (int)Math.Ceiling(GapPixels * scale);
            var usable = Math.Max(0, clientWidth - scrollbarAllowance);
            var columns = usable >= (int)Math.Ceiling(900f * scale) ? 3 : 2;
            var minimumCardWidth = (int)Math.Ceiling(280f * scale);
            var cardWidth = Math.Max(
                minimumCardWidth,
                (usable - (columns - 1) * gap) / columns);
            return new AccountGridMetrics
            {
                Columns = columns,
                CardWidth = cardWidth,
            };
        }
    }
}
