using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GpgPatcher.Gui
{
    internal enum RoundedSides
    {
        All,
        Left,
        Right,
        None,
    }

    internal static class RoundedDrawing
    {
        public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius, RoundedSides sides = RoundedSides.All)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || sides == RoundedSides.None)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            var diameter = Math.Min(Math.Min(bounds.Width, bounds.Height), radius * 2);
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            var roundLeft = sides != RoundedSides.Right;
            var roundRight = sides != RoundedSides.Left;

            AddCorner(path, arc, 180, roundLeft, new Point(bounds.Left, bounds.Top));
            arc.X = bounds.Right - diameter;
            AddCorner(path, arc, 270, roundRight, new Point(bounds.Right, bounds.Top));
            arc.Y = bounds.Bottom - diameter;
            AddCorner(path, arc, 0, roundRight, new Point(bounds.Right, bounds.Bottom));
            arc.X = bounds.Left;
            AddCorner(path, arc, 90, roundLeft, new Point(bounds.Left, bounds.Bottom));
            path.CloseFigure();
            return path;
        }

        private static void AddCorner(GraphicsPath path, Rectangle arc, float startAngle, bool rounded, Point squareCorner)
        {
            if (rounded)
            {
                path.AddArc(arc, startAngle, 90);
                return;
            }

            path.AddLine(squareCorner, squareCorner);
        }
    }
}
