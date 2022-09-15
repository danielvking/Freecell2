using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Wpf
{
    public class FreecellBoardViewModel : INotifyPropertyChanged
    {
        public FreecellBoardViewModel(int? seed = null)
        {
            _board = new FreecellBoard(seed);

            CardAt = new CardAtViewModel(this);
            CanMove = new CanMoveViewModel(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private FreecellBoard _board;
        public FreecellBoard Board
        {
            get { return _board; }
            private set
            {
                _board = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardAt)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanMove)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSolved)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seed)));
            }
        }

        public CardAtViewModel CardAt { get; }
        public CanMoveViewModel CanMove { get; }

        private Stack<int> _movesHistory = new Stack<int>();
        public int Moves => _movesHistory.Count > 0 ? _movesHistory.Peek() : 0;

        private Stack<int> _singleCardMovesHistory = new Stack<int>();
        public int SingleCardMoves => _singleCardMovesHistory.Count > 0 ? _singleCardMovesHistory.Peek() : 0;

        public bool IsSolved => Board.IsSolved();

        public bool CanUndo => Board.PreviousBoard != null;

        public int Seed => Board.Seed;

        public bool Move(Card from, int endRow, int endCol)
        {
            (var startRow, var startCol) = FindCard(from);

            if (MoveInternal(startRow, startCol, endRow, endCol)) return true;

            return false;
        }

        private bool MoveInternal(int startRow, int startCol, int endRow, int endCol, FreecellBoard expectedResult = null)
        {
            if (Board.CanMove(startRow, startCol, endRow, endCol))
            {
                var board = Board.Move(startRow, startCol, endRow, endCol);
                if (expectedResult == null || board.Equals(expectedResult))
                {
                    // Compute number of single-card moves
                    var singleCardMoves = 1;
                    if (startRow != 0 && endRow != 0)
                    {
                        var freeSpace = Enumerable.Range(0, 4).Count(x => Board[0, x] == Card.None);
                        var freeColumns = Enumerable.Range(0, 8).Count(x => Board[1, x] == Card.None && x != endCol);

                        var stackSize = 0;
                        var row = startRow;
                        while (Board[row++, startCol] != Card.None) stackSize++;

                        singleCardMoves = MulticardMoveHelper.SingleCardMoves(stackSize, freeSpace, freeColumns);
                    }

                    // Set board
                    Board = board;
                    _movesHistory.Push(Moves + 1);
                    _singleCardMovesHistory.Push(SingleCardMoves + singleCardMoves);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Moves)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SingleCardMoves)));
                    return true;
                }
            }
            return false;
        }

        public bool Move(FreecellBoard nextBoard)
        {
            for (int row = 0; row < 20; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var from = nextBoard[row, col];
                    if (from != Card.None && from != Board[row, col])
                    {
                        var endRow = row >= 1 ? 1 : 0;
                        var endCol = col;
                        (var startRow, var startCol) = FindCard(from);

                        return MoveInternal(startRow, startCol, endRow, endCol, nextBoard);
                    }
                }
            }

            return false;
        }

        private (int, int) FindCard(Card searchValue)
        {
            for (var row = 0; row < 20; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    if (searchValue.Equals(Board[row, col])) return (row, col);
                }
            }
            throw new InvalidOperationException($"The card ({searchValue}) cannot be found.");
        }

        public void MoveCardsHome()
        {
            var topCardRows = new int[8];
            for (int col = 0; col < 8; col++)
            {
                for (int row = 1; row < 20; row++)
                {
                    if (Board[row, col] != Card.None) topCardRows[col] = row;
                    else break;
                }
            }

            IEnumerable<Card> HomeCards()
            {
                yield return Board[0, 4];
                yield return Board[0, 5];
                yield return Board[0, 6];
                yield return Board[0, 7];
            }

            bool TryMove(int row, int col)
            {
                var card = Board[row, col];
                if (card == Card.None) return false;
                if (card.FaceValue() <= FaceValue.Two || HomeCards().Count(x => x != Card.None && card.FaceValue() <= x.FaceValue() + 2 && ((int)(x.Suit() ^ card.Suit()) & 1) == 1) == 2)
                {
                    if (MoveInternal(row, col, 0, 4)) return true;
                    if (MoveInternal(row, col, 0, 5)) return true;
                    if (MoveInternal(row, col, 0, 6)) return true;
                    if (MoveInternal(row, col, 0, 7)) return true;
                }
                return false;
            }

            bool keepGoing;
            do
            {
                keepGoing = false;
                for (int col = 0; col < 4; col++)
                {
                    if (TryMove(0, col))
                    {
                        keepGoing = true;
                    }
                }
                for (int col = 0; col < 8; col++)
                {
                    if (topCardRows[col] == 0) continue;
                    if (TryMove(topCardRows[col], col))
                    {
                        keepGoing = true;
                        topCardRows[col]--;
                    }
                }
            } while (keepGoing);
        }

        public void NewGame(int? seed = null)
        {
            Board = new FreecellBoard(seed);
            _movesHistory = new Stack<int>();
            _singleCardMovesHistory = new Stack<int>();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Moves)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SingleCardMoves)));
        }

        public void Undo()
        {
            if (CanUndo)
            {
                Board = Board.PreviousBoard;
                _movesHistory.Pop();
                _singleCardMovesHistory.Pop();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Moves)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SingleCardMoves)));
            }
        }

        public class CardAtViewModel
        {
            private readonly FreecellBoardViewModel _parent;
            internal CardAtViewModel(FreecellBoardViewModel parent)
            {
                _parent = parent;
            }

            public CardViewModel this[int row, int col]
            {
                get
                {
                    var card = _parent.Board[row, col];
                    return card != Card.None ? new CardViewModel(card) : null;
                }
            }
        }

        public class CanMoveViewModel
        {
            private readonly FreecellBoardViewModel _parent;
            internal CanMoveViewModel(FreecellBoardViewModel parent)
            {
                _parent = parent;
            }

            public bool this[int row, int col]
            {
                get => _parent.Board.CanMove(row, col);
            }
        }
    }
}
