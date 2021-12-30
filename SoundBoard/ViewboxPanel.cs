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

            _scale = Math.Min(1, availableSize.Height / height);

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
                    child.Arrange(new Rect(new Point(0, Math.Max(0, finalSize.Height / 2 - child.DesiredSize.Height / 2)), new Size(finalSize.Width / _scale, child.DesiredSize.Height)));
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
