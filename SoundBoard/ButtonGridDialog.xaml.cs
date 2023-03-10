#region Usings

using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

#endregion

namespace SoundBoard
{
    /// <summary>
    /// Interaction logic for ButtonGridDialog.xaml
    /// </summary>
    internal partial class ButtonGridDialog
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public ButtonGridDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startingRowCount"></param>
        /// <param name="startingColumnCount"></param>
        public ButtonGridDialog(int startingRowCount, int startingColumnCount) : this()
        {
            RowCount = _startingRowCount = startingRowCount;
            ColumnCount = _startingColumnCount = startingColumnCount;
        }

        public ButtonGridDialog(int startingRowCount, int startingColumnCount, string title, bool validate) : this(startingRowCount, startingColumnCount)
        {
            Title = title;
            _validate = validate;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The result of the dialog
        /// </summary>
        public DialogResult DialogResult;

        /// <summary>
        /// Number of rows
        /// </summary>
        public int RowCount
        {
            get => RowUpDown.Value ?? default;
            set => RowUpDown.Value = value;
        }

        /// <summary>
        /// Number of columns
        /// </summary>
        public int ColumnCount
        {
            get => ColumnUpDown.Value ?? default;
            set => ColumnUpDown.Value = value;
        }

        #endregion

        #region Private fields

        private readonly int _startingRowCount;

        private readonly int _startingColumnCount;

        #endregion

        #region Event handlers

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ColumnUpDown_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void RowUpDown_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void RowUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ShowHideWarningLabel();
        }

        private void ColumnUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ShowHideWarningLabel();
        }

        #endregion

        #region Private methods

        private void ShowHideWarningLabel()
        {
            if (_validate && WarningLabel is null == false)
            {
                WarningLabel.Visibility = RowUpDown.Value < _startingRowCount || ColumnUpDown.Value < _startingColumnCount
                        ? Visibility.Visible
                        : Visibility.Hidden;
            }
        }

        private readonly bool _validate = true;

        #endregion
    }
}
