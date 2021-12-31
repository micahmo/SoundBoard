using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SoundBoard
{
    /// <summary>
    /// A viewbox that scales vertically and stretches horizontally
    /// </summary>
    /// <remarks>
    /// Inspired by: https://stackoverflow.com/a/4543983/4206279
    /// </remarks>
    public class ViewboxPanel : Panel
    {
        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            double height = 0;
            Size unlimitedSize = new Size(availableSize.Width, double.PositiveInfinity);
            foreach (UIElement child in Children)
            {
                child.Measure(unlimitedSize);
                height += child.DesiredSize.Height;
            }

            // This prevents us from scaling UP, as a scale of 1 (100%) is our max
            var scale = Math.Min(1, availableSize.Height / height);
            
            // This prevents us from going to 0, which breaks things later. (Epsilon is the smallest positive double.)
            scale = Math.Max(double.Epsilon, scale);

            _scale = scale;

            return new Size(Math.Min(availableSize.Width, double.MaxValue - 1), Math.Min(availableSize.Height, double.MaxValue - 1));
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Transform scaleTransform = new ScaleTransform(_scale, _scale);
            double height = 0;
            foreach (UIElement child in Children)
            {
                child.RenderTransform = scaleTransform;
                try
                {
                    // This prevents us from going to double.Infinity, which occurs if we try to divide by Epsilon.
                    var newWidth = Math.Min(finalSize.Width / _scale, double.MaxValue);
                    child.Arrange(new Rect(new Point(0, Math.Max(0, finalSize.Height / 2 - child.DesiredSize.Height / 2)), new Size(newWidth, child.DesiredSize.Height)));
                }
                catch
                {
                    // Handle any NaN, divide by 0, etc. errors. Just make it invisible.
                    child.Arrange(new Rect(new Point(0, 0), new Size(0, 0)));
                }

                height += child.DesiredSize.Height;
            }

            return finalSize;
        }

        private double _scale;
    }

}
