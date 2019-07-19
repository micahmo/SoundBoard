#region Usings

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Point = System.Windows.Point;

#endregion

namespace SoundBoard
{
    internal sealed class MyMetroTabItem : MetroTabItem
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MyMetroTabItem() : base()
        {
            AllowDrop = true;
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _mouseDownPosition = Mouse.GetPosition(this);
            }

            base.OnMouseDown(e);
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _mouseDownPosition = null;
            }

            base.OnMouseUp(e);
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseDownPosition is null == false &&
                Utilities.PointsArePastThreshold((Point)_mouseDownPosition, Mouse.GetPosition(this), 0))
            {
                _mouseDownPosition = Mouse.GetPosition(this);

                // First, remove the tab so the user can "drag" it
                int index = ParentTabControl.Items.IndexOf(this);
                ParentTabControl.Items.Remove(this);

                // Set up our UI for the drag operation
                CreateDragDropWindow(Mouse.DirectlyOver as Visual);
                CreateDropTabs(index * 2);

                // Do the drag operation
                DragDrop.DoDragDrop(this, new TabDragData {SourceTab = this}, DragDropEffects.Link);

                // Find where to drop the tab
                int dropIndex = ParentTabControl.Items.OfType<MyMetroTabItem>().ToList().IndexOf(ParentTabControl.Items
                    .OfType<MyMetroTabItem>().FirstOrDefault(tab => tab.IsFocused)) / 2;

                // Clean up our UI from the drag operation
                DestroyDragDropWindow();
                DestroyDropTabs();

                // Put the dragged tab where the user dropped it
                ParentTabControl.Items.Insert(dropIndex, this);

                // Re-focus the dragged tab
                ParentTabControl.Items.OfType<MetroTabItem>().ElementAt(dropIndex).Focus();
            }

            base.OnMouseMove(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _mouseDownPosition = null;

            base.OnMouseLeave(e);
        }

        /// <inheritdoc />
        protected override void OnDragEnter(DragEventArgs e)
        {
            Focus();
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            if (_dragDropWindow is null == false)
            {
                Utilities.Win32Point mouse = new Utilities.Win32Point();
                Utilities.GetCursorPos(ref mouse);
                _dragDropWindow.Left = mouse.X;
                _dragDropWindow.Top = mouse.Y;
            }
        }

        /// <inheritdoc />
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            if (Tag?.ToString() == DROP_TAB_TAG)
            {
                Header = DropTabFocusedHeader;
            }
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (Tag?.ToString() == DROP_TAB_TAG)
            {
                Header = DropTabHeader;
            }
        }

        #endregion

        #region Private properties

        private TabControl ParentTabControl => _parentTabControl ?? 
                                               (_parentTabControl = Parent as TabControl) ??
                                               throw new System.Exception($@"{nameof(MyMetroTabItem)}: Unable to set {nameof(ParentTabControl)}");

        private Image DropTabHeader => ImageHelper.GetImage(ImageHelper.AddButtonPath, 25, 25);

        private Image DropTabFocusedHeader => ImageHelper.GetImage(ImageHelper.AddFocusButtonPath, 25, 25);

        #endregion

        #region Private fields

        private Point? _mouseDownPosition;

        private TabControl _parentTabControl;

        private Window _dragDropWindow;

        #endregion

        #region Private methods

        /// <remarks>Adapted from https://stackoverflow.com/a/27975085/4206279 </remarks>
        private void CreateDragDropWindow(Visual dragElement)
        {
            FrameworkElement frameworkElement = (FrameworkElement) dragElement;
            int width = (int)frameworkElement.ActualWidth;
            int height = (int)frameworkElement.ActualHeight;

            _dragDropWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                AllowDrop = false,
                Background = null,
                IsHitTestVisible = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true,
                ShowInTaskbar = false
            };

            RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dragElement);

            _dragDropWindow.Content = new Image {Source = bmp, Width = width, Height = height};

            Utilities.Win32Point mouse = new Utilities.Win32Point();
            Utilities.GetCursorPos(ref mouse);

            _dragDropWindow.Left = mouse.X;
            _dragDropWindow.Top = mouse.Y;
            _dragDropWindow.Show();
        }

        private void DestroyDragDropWindow()
        {
            _dragDropWindow?.Close();
            _dragDropWindow = null;
        }

        private void CreateDropTabs(int dropTabToFocus = 0)
        {
            // Disallow drop for existing tabs
            foreach (MetroTabItem metroTabItem in ParentTabControl.Items)
            {
                metroTabItem.AllowDrop = false;
            }

            // Add our drop tabs
            int count = ParentTabControl.Items.Count;
            for (int i = 0; i < count; ++i)
            {
                ParentTabControl.Items.Insert(i * 2, new MyMetroTabItem { Header = DropTabHeader, Tag = DROP_TAB_TAG });
            }
            ParentTabControl.Items.Add(new MyMetroTabItem { Header = DropTabHeader, Tag = DROP_TAB_TAG });

            ParentTabControl.Items.OfType<MetroTabItem>().ElementAt(dropTabToFocus).Focus();
        }

        private void DestroyDropTabs()
        {
            // Remove our drop tabs
            for (int i = ParentTabControl.Items.Count - 1; i >= 0; --i)
            {
                if (((MetroTabItem) ParentTabControl.Items[i]).Tag?.ToString() == DROP_TAB_TAG)
                {
                    ParentTabControl.Items.RemoveAt(i);
                }
            }

            // Re-allow drop
            foreach (MetroTabItem metroTabItem in ParentTabControl.Items)
            {
                metroTabItem.AllowDrop = true;
            }
        }

        #endregion

        #region Consts

        private const string DROP_TAB_TAG = nameof(DROP_TAB_TAG);

        #endregion
    }

    #region TabDragData class

    internal class TabDragData
    {
        public MyMetroTabItem SourceTab { get; set; }
    }

    #endregion
}
