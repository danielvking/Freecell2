using Freecell.Solver;
using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Freecell.Wpf
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            FreecellBoard = new FreecellBoardViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private FreecellBoard _solution;

        public FreecellBoardViewModel FreecellBoard { get; set; }

        public bool IsLoading { get; set; }

        public string StatusMessage { get; set; }

        public bool CanAnything => !IsLoading;

        public bool CanUndo => CanAnything && FreecellBoard.CanUndo;

        private void RaisePropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        internal bool Move(Card from, int endRow, int endCol)
        {
            StatusMessage = null;
            RaisePropertyChanged(nameof(StatusMessage));
            return FreecellBoard.Move(from, endRow, endCol);
        }

        internal void MoveCardsHome()
        {
            StatusMessage = null;
            RaisePropertyChanged(nameof(StatusMessage));
            FreecellBoard.MoveCardsHome();
        }

        internal void NewGame(int? seed)
        {
            if (CanUndo && !FreecellBoard.IsSolved)
            {
                var result = System.Windows.MessageBox.Show("Are you sure you want to stop this game and start a new one?", "Giving up?", System.Windows.MessageBoxButton.YesNo);
                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }
            }
            FreecellBoard.NewGame(seed);
        }

        internal bool ShouldClose()
        {
            if (CanUndo && !FreecellBoard.IsSolved)
            {
                var result = System.Windows.MessageBox.Show("Are you sure you want to stop this game?", "Giving up?", System.Windows.MessageBoxButton.YesNo);
                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    return false;
                }
            }
            return true;
        }

        internal void Undo()
        {
            StatusMessage = null;
            RaisePropertyChanged(nameof(StatusMessage));
            if (CanUndo) FreecellBoard.Undo();
        }

        async internal void Hint()
        {
            if (FreecellBoard.IsSolved) {
                StatusMessage = "Board is already solved...";
                RaisePropertyChanged(nameof(StatusMessage));
                return;
            }

            FreecellBoard NextBoard(FreecellBoard solution)
            {
                var previous = solution;
                while (previous != null)
                {
                    var current = previous.PreviousBoard;
                    if (FreecellBoard.Board.Equals(current)) return previous;
                    previous = current;
                }
                return null;
            }

            FreecellBoard nextBoard = NextBoard(_solution);
            
            if (nextBoard == null)
            {
                IsLoading = true;
                StatusMessage = "This may take a minute...";
                RaisePropertyChanged(nameof(IsLoading));
                RaisePropertyChanged(nameof(StatusMessage));

                var adapterOptions = new Action<FreecellAStarAdapterOptions>[]
                {
                    options => options.HeuristicFunction = x => Heuristics.BasicHeuristic(x),
                    options => options.HeuristicFunction = x => Heuristics.BasicHeuristic(x) * 4,
                    options => options.HeuristicFunction = x => Heuristics.LooserHeuristic(x),
                    options => options.HeuristicFunction = x => Heuristics.LooserHeuristic(x) * 2,
                    options => options.HeuristicFunction = x => Heuristics.LooserHeuristic(x) * 3,
                    options => options.HeuristicFunction = x => Heuristics.WeightedLooserHeuristic(x),
                    options => options.HeuristicFunction = x => Heuristics.AdvancedHeuristic(x),
                    options => options.HeuristicFunction = x => Heuristics.AdvancedHeuristic(x) << 2
                };

                var adapters = adapterOptions.Select(x => FreecellAStarAdapter.Create(FreecellBoard.Board, x));

                using (var cts = new CancellationTokenSource())
                {
                    var solverTasks = adapters.Select(x => Task.Run(() => AStarSolver.Solve(x, cts.Token))).ToList();
                    var solvedTask = await Task.WhenAny(solverTasks);
                    cts.Cancel();
                    await Task.WhenAll(solverTasks);

                    _solution = (await solvedTask)?.Board;
                }

                IsLoading = false;
                RaisePropertyChanged(nameof(IsLoading));

                nextBoard = NextBoard(_solution);
            }

            if (nextBoard == null)
            {
                var message = "There is no way to win this game. " + (FreecellBoard.CanUndo ? "You will have to undo to proceed." : "That's especially impressive, considering you haven't moved any cards.");
                MessageBox.Show(message, "Oof");
                StatusMessage = "You're going to need to tickle that undo button...";
                RaisePropertyChanged(nameof(StatusMessage));
            }
            else if (FreecellBoard.Move(nextBoard))
            {
                StatusMessage = null;
                RaisePropertyChanged(nameof(StatusMessage));
            }
            else
            {
                _solution = null;
                Hint();
            }
        }
    }
}
