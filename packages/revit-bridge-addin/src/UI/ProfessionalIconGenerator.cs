using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RevitBridge.UI
{
    /// <summary>
    /// Professional icon generator with modern gradients and branding
    /// </summary>
    public static class ProfessionalIconGenerator
    {
        // Brand colors - Modern tech aesthetic
        private static readonly Color BrandPrimary = Color.FromRgb(96, 125, 255);      // Vibrant Blue
        private static readonly Color BrandSecondary = Color.FromRgb(58, 77, 155);    // Deep Blue
        private static readonly Color AccentGreen = Color.FromRgb(52, 199, 89);       // Success Green
        private static readonly Color AccentRed = Color.FromRgb(255, 59, 48);         // Alert Red
        private static readonly Color AccentOrange = Color.FromRgb(255, 149, 0);      // Warning Orange

        /// <summary>
        /// Enhanced Connect icon with gradient and modern design
        /// </summary>
        public static BitmapSource CreateConnectIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new RadialGradientBrush();
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(82, 229, 119), 0));
                gradient.GradientStops.Add(new GradientStop(AccentGreen, 1));
                gradient.Center = new Point(0.5, 0.4);

                // Circle with subtle shadow
                var shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                context.DrawEllipse(shadowBrush, null,
                    new Point(size / 2.0 + 1, size / 2.0 + 1),
                    size / 2.0, size / 2.0);

                context.DrawEllipse(gradient, null,
                    new Point(size / 2.0, size / 2.0),
                    size / 2.0, size / 2.0);

                // Play symbol with subtle glow
                var whiteBrush = Brushes.White;
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(size * 0.38, size * 0.28) };
                figure.Segments.Add(new LineSegment(new Point(size * 0.38, size * 0.72), true));
                figure.Segments.Add(new LineSegment(new Point(size * 0.72, size * 0.5), true));
                figure.IsClosed = true;
                geometry.Figures.Add(figure);
                context.DrawGeometry(whiteBrush, null, geometry);

                // Network nodes (IoT/connectivity theme)
                double dotSize = size * 0.06;
                context.DrawEllipse(whiteBrush, null, new Point(size * 0.18, size * 0.18), dotSize, dotSize);
                context.DrawEllipse(whiteBrush, null, new Point(size * 0.82, size * 0.18), dotSize, dotSize);
                context.DrawEllipse(whiteBrush, null, new Point(size * 0.82, size * 0.82), dotSize, dotSize);

                // Connection lines
                var linePen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), size * 0.02);
                context.DrawLine(linePen, new Point(size * 0.18, size * 0.18), new Point(size * 0.5, size * 0.35));
                context.DrawLine(linePen, new Point(size * 0.82, size * 0.18), new Point(size * 0.65, size * 0.35));
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Enhanced Disconnect icon with gradient
        /// </summary>
        public static BitmapSource CreateDisconnectIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new RadialGradientBrush();
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 99, 88), 0));
                gradient.GradientStops.Add(new GradientStop(AccentRed, 1));
                gradient.Center = new Point(0.5, 0.4);

                // Circle with shadow
                var shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                context.DrawEllipse(shadowBrush, null,
                    new Point(size / 2.0 + 1, size / 2.0 + 1),
                    size / 2.0, size / 2.0);

                context.DrawEllipse(gradient, null,
                    new Point(size / 2.0, size / 2.0),
                    size / 2.0, size / 2.0);

                // Stop square with rounded corners
                var whiteBrush = Brushes.White;
                var rect = new RectangleGeometry(
                    new Rect(size * 0.32, size * 0.32, size * 0.36, size * 0.36),
                    size * 0.04, size * 0.04);  // Rounded corners
                context.DrawGeometry(whiteBrush, null, rect);
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Enhanced Status icon with analytics chart
        /// </summary>
        public static BitmapSource CreateStatusIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new RadialGradientBrush();
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(136, 165, 255), 0));
                gradient.GradientStops.Add(new GradientStop(BrandPrimary, 1));
                gradient.Center = new Point(0.5, 0.4);

                // Circle with shadow
                var shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                context.DrawEllipse(shadowBrush, null,
                    new Point(size / 2.0 + 1, size / 2.0 + 1),
                    size / 2.0, size / 2.0);

                context.DrawEllipse(gradient, null,
                    new Point(size / 2.0, size / 2.0),
                    size / 2.0, size / 2.0);

                // Modern chart bars with gradient
                var whiteBrush = Brushes.White;
                double barWidth = size * 0.14;
                double spacing = size * 0.06;
                double startX = size * 0.22;
                double baseY = size * 0.72;

                // Three bars with rounded tops
                var bar1 = new RectangleGeometry(
                    new Rect(startX, baseY - size * 0.18, barWidth, size * 0.18),
                    barWidth / 4, barWidth / 4);
                var bar2 = new RectangleGeometry(
                    new Rect(startX + barWidth + spacing, baseY - size * 0.30, barWidth, size * 0.30),
                    barWidth / 4, barWidth / 4);
                var bar3 = new RectangleGeometry(
                    new Rect(startX + (barWidth + spacing) * 2, baseY - size * 0.42, barWidth, size * 0.42),
                    barWidth / 4, barWidth / 4);

                context.DrawGeometry(whiteBrush, null, bar1);
                context.DrawGeometry(whiteBrush, null, bar2);
                context.DrawGeometry(whiteBrush, null, bar3);

                // Base line
                var basePen = new Pen(whiteBrush, size * 0.02);
                context.DrawLine(basePen, new Point(size * 0.2, baseY), new Point(size * 0.8, baseY));
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Enhanced Settings icon (modern gear)
        /// </summary>
        public static BitmapSource CreateSettingsIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new RadialGradientBrush();
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(170, 170, 170), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(130, 130, 130), 1));
                gradient.Center = new Point(0.5, 0.4);

                // Circle with shadow
                var shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                context.DrawEllipse(shadowBrush, null,
                    new Point(size / 2.0 + 1, size / 2.0 + 1),
                    size / 2.0, size / 2.0);

                context.DrawEllipse(gradient, null,
                    new Point(size / 2.0, size / 2.0),
                    size / 2.0, size / 2.0);

                // Modern gear icon
                var whiteBrush = Brushes.White;

                // Outer gear (simplified modern design)
                var gearPath = new PathGeometry();
                for (int i = 0; i < 6; i++)
                {
                    double angle = i * 60 * Math.PI / 180;
                    double x = size / 2.0 + Math.Cos(angle) * size * 0.35;
                    double y = size / 2.0 + Math.Sin(angle) * size * 0.35;

                    context.DrawEllipse(whiteBrush, null, new Point(x, y), size * 0.08, size * 0.08);
                }

                // Center circle
                context.DrawEllipse(whiteBrush, null,
                    new Point(size / 2.0, size / 2.0),
                    size * 0.15, size * 0.15);

                // Inner hollow
                var innerBrush = new SolidColorBrush(Color.FromRgb(130, 130, 130));
                context.DrawEllipse(innerBrush, null,
                    new Point(size / 2.0, size / 2.0),
                    size * 0.08, size * 0.08);
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Enhanced Brand icon (RevitMCP logo)
        /// </summary>
        public static BitmapSource CreateBrandIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new LinearGradientBrush();
                gradient.StartPoint = new Point(0, 0);
                gradient.EndPoint = new Point(1, 1);
                gradient.GradientStops.Add(new GradientStop(BrandPrimary, 0));
                gradient.GradientStops.Add(new GradientStop(BrandSecondary, 1));

                // Rounded square for modern look
                var bgRect = new RectangleGeometry(
                    new Rect(0, 0, size, size),
                    size * 0.12, size * 0.12);  // Rounded corners
                context.DrawGeometry(gradient, null, bgRect);

                // "MCP" monogram in modern typography
                var whiteBrush = Brushes.White;
                var textFormat = new FormattedText(
                    "MCP",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    size * 0.35,
                    whiteBrush,
                    1.0);

                // Center the text
                double textX = (size - textFormat.Width) / 2;
                double textY = (size - textFormat.Height) / 2;
                context.DrawText(textFormat, new Point(textX, textY));

                // Small AI indicator dot
                var accentDot = new RadialGradientBrush();
                accentDot.GradientStops.Add(new GradientStop(Color.FromRgb(82, 229, 119), 0));
                accentDot.GradientStops.Add(new GradientStop(AccentGreen, 1));
                context.DrawEllipse(accentDot, null,
                    new Point(size * 0.82, size * 0.18),
                    size * 0.08, size * 0.08);
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Enhanced Help icon
        /// </summary>
        public static BitmapSource CreateHelpIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new RadialGradientBrush();
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(138, 180, 248), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(93, 156, 236), 1));
                gradient.Center = new Point(0.5, 0.4);

                // Circle with shadow
                var shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                context.DrawEllipse(shadowBrush, null,
                    new Point(size / 2.0 + 1, size / 2.0 + 1),
                    size / 2.0, size / 2.0);

                context.DrawEllipse(gradient, null,
                    new Point(size / 2.0, size / 2.0),
                    size / 2.0, size / 2.0);

                // Question mark
                var whiteBrush = Brushes.White;
                var textFormat = new FormattedText(
                    "?",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    size * 0.65,
                    whiteBrush,
                    1.0);

                double textX = (size - textFormat.Width) / 2;
                double textY = (size - textFormat.Height) / 2 - size * 0.02;
                context.DrawText(textFormat, new Point(textX, textY));
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Enhanced About icon
        /// </summary>
        public static BitmapSource CreateAboutIcon(int size = 32)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Gradient background
                var gradient = new RadialGradientBrush();
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(138, 180, 248), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(93, 156, 236), 1));
                gradient.Center = new Point(0.5, 0.4);

                // Circle with shadow
                var shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                context.DrawEllipse(shadowBrush, null,
                    new Point(size / 2.0 + 1, size / 2.0 + 1),
                    size / 2.0, size / 2.0);

                context.DrawEllipse(gradient, null,
                    new Point(size / 2.0, size / 2.0),
                    size / 2.0, size / 2.0);

                // Info "i"
                var whiteBrush = Brushes.White;
                var textFormat = new FormattedText(
                    "i",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    size * 0.65,
                    whiteBrush,
                    1.0);

                double textX = (size - textFormat.Width) / 2;
                double textY = (size - textFormat.Height) / 2 - size * 0.02;
                context.DrawText(textFormat, new Point(textX, textY));
            }

            return RenderVisual(visual, size, size);
        }

        /// <summary>
        /// Render DrawingVisual to BitmapSource
        /// </summary>
        private static BitmapSource RenderVisual(DrawingVisual visual, int width, int height)
        {
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }

        /// <summary>
        /// Save icon to PNG file
        /// </summary>
        public static void SaveIcon(BitmapSource bitmap, string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
    }
}
