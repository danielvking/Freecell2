using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Freecell.Wpf
{
    /// <summary>
    /// Interaction logic for SelectGameWindow.xaml
    /// </summary>
    public partial class SelectGameWindow : Window
    {
        public SelectGameWindow()
        {
            InitializeComponent();
        }

        public int? GameNumber {
            get { return (int?)GetValue(GameNumberProperty); }
            set { SetValue(GameNumberProperty, value); }
        }
        
        public static readonly DependencyProperty GameNumberProperty =
            DependencyProperty.Register("GameNumber", typeof(int?), typeof(SelectGameWindow));

        private readonly Regex numeric = new Regex("[0-9]+");
        private bool IsTextAllowed(string text)
        {
            return numeric.IsMatch(text);
        }

        private void TextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
