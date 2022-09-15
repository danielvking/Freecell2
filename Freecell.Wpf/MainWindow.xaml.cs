using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Freecell.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double _originalX;
        private double _originalY;
        private double _positionDeltaX;
        private double _positionDeltaY;
        private UIElement _held;

        public static readonly RoutedCommand HintCommand = new RoutedCommand("Hint", typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetCard(card010);
            ResetCard(card011);
            ResetCard(card012);
            ResetCard(card013);
            ResetCard(card014);
            ResetCard(card015);
            ResetCard(card016);
            ResetCard(card017);
        }

        public static bool GetDraggable(DependencyObject obj)
        {
            return (bool)obj.GetValue(DraggableProperty);
        }

        public static void SetDraggable(DependencyObject obj, bool value)
        {
            obj.SetValue(DraggableProperty, value);
        }
        
        public static readonly DependencyProperty DraggableProperty =
            DependencyProperty.RegisterAttached("Draggable", typeof(bool), typeof(MainWindow));

        public static UIElement GetNextCard(UIElement obj)
        {
            return (UIElement)obj.GetValue(NextCardProperty);
        }

        public static void SetNextCard(UIElement obj, UIElement value)
        {
            obj.SetValue(NextCardProperty, value);
        }
        
        public static readonly DependencyProperty NextCardProperty =
            DependencyProperty.RegisterAttached("NextCard", typeof(UIElement), typeof(MainWindow));

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) DropHeld();

            var mousePosition = e.MouseDevice.GetPosition(canvas);
            MoveHeld(mousePosition);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            if (source == null || source == sender) return;

            var parent = VisualTreeHelper.GetParent(source);
            while (parent != canvas && parent != sender)
            {
                source = parent;
                parent = VisualTreeHelper.GetParent(source);
            }

            if (source is ContentControl card && GetDraggable(card))
            {
                if (e.ClickCount == 2)
                {
                    var viewModel = DataContext as MainWindowViewModel;

                    var from = ((CardViewModel)card.Content).Card;

                    if (viewModel.Move(from, 0, 4) || viewModel.Move(from, 0, 5) || viewModel.Move(from, 0, 6) || viewModel.Move(from, 0, 7))
                    {
                        viewModel.MoveCardsHome();
                    }
                }
                else
                {
                    var mousePosition = e.GetPosition(canvas);
                    SetHeld(mousePosition, card);
                }
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DropHeld(e);
        }

        private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            if (source == null || source == sender) return;

            var parent = VisualTreeHelper.GetParent(source);
            while (parent != canvas && parent != sender)
            {
                source = parent;
                parent = VisualTreeHelper.GetParent(source);
            }

            if (source is ContentControl card)
            {

                var viewModel = DataContext as MainWindowViewModel;

                var from = ((CardViewModel)card.Content).Card;

                if (card == card000 || card == card001 || card == card002 || card == card003)
                {
                    if (viewModel.Move(from, 1, 0)) return;
                    if (viewModel.Move(from, 1, 1)) return;
                    if (viewModel.Move(from, 1, 2)) return;
                    if (viewModel.Move(from, 1, 3)) return;
                    if (viewModel.Move(from, 1, 4)) return;
                    if (viewModel.Move(from, 1, 5)) return;
                    if (viewModel.Move(from, 1, 6)) return;
                    if (viewModel.Move(from, 1, 7)) return;
                }
                else
                {
                    if (viewModel.Move(from, 0, 0)) return;
                    if (viewModel.Move(from, 0, 1)) return;
                    if (viewModel.Move(from, 0, 2)) return;
                    if (viewModel.Move(from, 0, 3)) return;
                }
            }
        }

        private void ResetCard(UIElement card)
        {
            SetHeld(default(Point), card);
            DropHeld();
        }

        private void SetHeld(Point mousePosition, UIElement card)
        {
            _held = card;
            _originalX = Canvas.GetLeft(_held);
            _originalY = Canvas.GetTop(_held);
            _positionDeltaX = Canvas.GetLeft(_held) - mousePosition.X;
            _positionDeltaY = Canvas.GetTop(_held) - mousePosition.Y;
        }

        private void MoveHeld(Point mousePosition)
        {
            if (_held != null)
            {
                var newX = mousePosition.X + _positionDeltaX;
                var newY = mousePosition.Y + _positionDeltaY;

                var nextCard = _held;
                var depth = 0;
                do
                {
                    Canvas.SetZIndex(nextCard, 1);
                    Canvas.SetLeft(nextCard, newX);
                    Canvas.SetTop(nextCard, newY + 20 * depth++);
                }
                while ((nextCard = GetNextCard(nextCard)) != null);
            }
        }

        private void DropHeld(MouseButtonEventArgs e = null)
        {
            if (_held != null)
            {
                if (e != null) ProcessCardMove(e);

                var nextCard = _held;
                var depth = 0;
                do
                {
                    Canvas.SetZIndex(nextCard, 0);
                    Canvas.SetLeft(nextCard, _originalX);
                    Canvas.SetTop(nextCard, _originalY + 20 * depth++);
                }
                while ((nextCard = GetNextCard(nextCard)) != null);

                _held = null;
            }
        }

        private void ProcessCardMove(MouseButtonEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            var mousePosition = e.GetPosition(canvas);
            var heldLeft = mousePosition.X + _positionDeltaX;
            var heldCenterY = mousePosition.Y + _positionDeltaY + 24;

            var halfCardWidth = 24;

            var from = ((CardViewModel)(_held as ContentControl).Content).Card;
            if (heldCenterY < 112)
            {
                if (Math.Abs(heldLeft - 32) < halfCardWidth) viewModel.Move(from, 0, 0);
                else if (Math.Abs(heldLeft - 96) < halfCardWidth) viewModel.Move(from, 0, 1);
                else if (Math.Abs(heldLeft - 160) < halfCardWidth) viewModel.Move(from, 0, 2);
                else if (Math.Abs(heldLeft - 224) < halfCardWidth) viewModel.Move(from, 0, 3);
                else if (Math.Abs(heldLeft - 368) < halfCardWidth) viewModel.Move(from, 0, 4);
                else if (Math.Abs(heldLeft - 432) < halfCardWidth) viewModel.Move(from, 0, 5);
                else if (Math.Abs(heldLeft - 496) < halfCardWidth) viewModel.Move(from, 0, 6);
                else if (Math.Abs(heldLeft - 560) < halfCardWidth) viewModel.Move(from, 0, 7);
            }
            else
            {
                if (Math.Abs(heldLeft - 72) < halfCardWidth) viewModel.Move(from, 1, 0);
                else if (Math.Abs(heldLeft - 136) < halfCardWidth) viewModel.Move(from, 1, 1);
                else if (Math.Abs(heldLeft - 200) < halfCardWidth) viewModel.Move(from, 1, 2);
                else if (Math.Abs(heldLeft - 264) < halfCardWidth) viewModel.Move(from, 1, 3);
                else if (Math.Abs(heldLeft - 328) < halfCardWidth) viewModel.Move(from, 1, 4);
                else if (Math.Abs(heldLeft - 392) < halfCardWidth) viewModel.Move(from, 1, 5);
                else if (Math.Abs(heldLeft - 456) < halfCardWidth) viewModel.Move(from, 1, 6);
                else if (Math.Abs(heldLeft - 520) < halfCardWidth) viewModel.Move(from, 1, 7);
            }
        }

        private void Anything_Can(object sender, CanExecuteRoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            e.CanExecute = viewModel.CanAnything;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            viewModel.NewGame(null);
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var helperWindow = new SelectGameWindow();
            helperWindow.Owner = this;
            var result = helperWindow.ShowDialog();
            if (result == true && helperWindow.GameNumber.HasValue)
            {
                var viewModel = DataContext as MainWindowViewModel;

                viewModel.NewGame(helperWindow.GameNumber);
            }
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            if (!viewModel.ShouldClose())
            {
                e.Cancel = true;
            }
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            e.CanExecute = viewModel.CanUndo;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            viewModel.Undo();
        }

        private void Hint_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            viewModel.Hint();
        }
    }
}
