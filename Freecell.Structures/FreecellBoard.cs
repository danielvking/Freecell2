using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Structures
{
    public class FreecellBoard
    {
        public FreecellBoard(int? seed = null)
        {
            _board = new byte[8][] { new byte[8], new byte[8], new byte[8], new byte[8], new byte[7], new byte[7], new byte[7], new byte[7] };

            Seed = seed ?? new Random().Next(10000000);
            var rand = new MicrosoftSolitaireRng(Seed);

            var deck = new List<Card>();
            for (var faceValue = FaceValue.Ace; faceValue <= FaceValue.King; faceValue++)
            {
                deck.Add(CardExtensions.Create(faceValue, Suit.Club));
                deck.Add(CardExtensions.Create(faceValue, Suit.Diamond));
                deck.Add(CardExtensions.Create(faceValue, Suit.Heart));
                deck.Add(CardExtensions.Create(faceValue, Suit.Spade));
            }

            var row = 1;
            var col = 0;
            for (int i = 52; i > 0; i--)
            {
                var randex = rand.Next(i);
                _board[col][row] = (byte)deck[randex];
                deck[randex] = deck[i - 1];
                col++;
                if (col == 8)
                {
                    row++;
                    col = 0;
                }
            }
        }

        public FreecellBoard(Card[] cards)
        {
            if (cards.Length != 52 || cards.Distinct().Count() != 52 || cards.Any(x => x < Card.AceHeart || x > Card.KingClub))
                throw new ArgumentException("The provided list must contain every card exactly once.");

            _board = new byte[8][] { new byte[8], new byte[8], new byte[8], new byte[8], new byte[7], new byte[7], new byte[7], new byte[7] };

            var row = 1;
            var col = 0;
            for (int i = 0; i < 52; i++)
            {
                _board[col][row] = (byte)cards[i];
                col++;
                if (col == 8)
                {
                    row++;
                    col = 0;
                }
            }
        }

        private FreecellBoard(FreecellBoard parent, byte[][] cards)
        {
            PreviousBoard = parent;
            _board = cards;
            Seed = parent.Seed;
        }

        public int Seed { get; }

        public FreecellBoard PreviousBoard { get; }

        private readonly byte[][] _board;

        public Card this[int row, int col]
        {
            get
            {
                var cardCol = _board[col];
                return row < cardCol.Length ? (Card)cardCol[row] : Card.None;
            }
        }

        private int? _moveableStackSize;
        private int MoveableStackSize {
            get
            {
                if (!_moveableStackSize.HasValue)
                {
                    int freeSpace = 0;
                    int freeStacks = 0;

                    if (_board[0][0] == 0) freeSpace++;
                    if (_board[1][0] == 0) freeSpace++;
                    if (_board[2][0] == 0) freeSpace++;
                    if (_board[3][0] == 0) freeSpace++;
                    if (_board[0].Length == 1) freeStacks++;
                    if (_board[1].Length == 1) freeStacks++;
                    if (_board[2].Length == 1) freeStacks++;
                    if (_board[3].Length == 1) freeStacks++;
                    if (_board[4].Length == 1) freeStacks++;
                    if (_board[5].Length == 1) freeStacks++;
                    if (_board[6].Length == 1) freeStacks++;
                    if (_board[7].Length == 1) freeStacks++;

                    _moveableStackSize = (freeSpace + 1) << freeStacks;
                }
                return _moveableStackSize.Value;
            }
        }

        private byte[] _normalizedColumnLocations;
        private byte[] NormalizedColumnLocations {
            get
            {
                if (_normalizedColumnLocations == null)
                {
                    _normalizedColumnLocations = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };

                    // Cache cards and assign values to empty spaces
                    var cards = new byte[]
                    {
                        _board[0].Length > 1 ? _board[0][1] : (byte)0,
                        _board[1].Length > 1 ? _board[1][1] : (byte)0,
                        _board[2].Length > 1 ? _board[2][1] : (byte)0,
                        _board[3].Length > 1 ? _board[3][1] : (byte)0,
                        _board[4].Length > 1 ? _board[4][1] : (byte)0,
                        _board[5].Length > 1 ? _board[5][1] : (byte)0,
                        _board[6].Length > 1 ? _board[6][1] : (byte)0,
                        _board[7].Length > 1 ? _board[7][1] : (byte)0
                    };

                    // Insertion sort second row
                    for (byte i = 1; i < 8; i++)
                    {
                        var card = cards[i];
                        for (byte j = i; j >= 0; j--)
                        {
                            if (j == 0 || card >= cards[_normalizedColumnLocations[j - 1]])
                            {
                                _normalizedColumnLocations[j] = i;
                                break;
                            }
                            _normalizedColumnLocations[j] = _normalizedColumnLocations[j - 1];
                        }
                    }
                }
                return _normalizedColumnLocations;
            }
        }

        public FreecellBoard Move(int startRow, int startCol, int endRow, int endCol)
        {
            if (!CanMove(startRow, startCol, endRow, endCol)) throw new InvalidOperationException($"The requested move is invalid.");

            var board = (byte[][])_board.Clone();

            if (endRow == 0)
            {
                if (startRow == 0)
                {
                    board[startCol] = (byte[])_board[startCol].Clone();
                    board[endCol] = (byte[])_board[endCol].Clone();
                    board[startCol][startRow] = 0;
                }
                else
                {
                    if (startCol != endCol) board[endCol] = (byte[])_board[endCol].Clone();
                    board[startCol] = new byte[startRow];
                    Buffer.BlockCopy(_board[startCol], 0, board[startCol], 0, startRow);
                }
                board[endCol][endRow] = _board[startCol][startRow];
            }
            else
            {
                endRow = _board[endCol].Length;
                if (startRow == 0)
                {
                    if (startCol != endCol) board[startCol] = (byte[])_board[startCol].Clone();
                    board[endCol] = new byte[endRow + 1];
                    Buffer.BlockCopy(_board[endCol], 0, board[endCol], 0, endRow);
                    board[startCol][startRow] = 0;
                    board[endCol][endRow] = _board[startCol][startRow];
                }
                else
                {
                    board[startCol] = new byte[startRow];
                    Buffer.BlockCopy(_board[startCol], 0, board[startCol], 0, startRow);
                    board[endCol] = new byte[endRow + _board[startCol].Length - startRow];
                    Buffer.BlockCopy(_board[endCol], 0, board[endCol], 0, endRow);
                    do
                    {
                        var temp = _board[startCol][startRow];
                        board[endCol][endRow] = temp;
                        endRow++;
                        startRow++;
                    } while (endRow < board[endCol].Length);
                }
            }

            return new FreecellBoard(this, board);
        }

        public bool CanMove(int row, int col)
        {
            if (row < 0 || row >= 20 || col < 0 || col >= 8)
            {
                throw new ArgumentOutOfRangeException($"The requested position ({row}, {col}) does not exist.");
            }

            if (row == 0) return col < 4 && _board[col][row] != 0;

            var cannotMoveRow = row + MoveableStackSize;
            if (cannotMoveRow < _board[col].Length) return false;

            if (row >= _board[col].Length) return false;
            var lastCard = (Card)_board[col][row];
            for (int r = row + 1; r < _board[col].Length; r++)
            {
                var card = (Card)_board[col][r];

                if (lastCard.FaceValue() - card.FaceValue() != 1 || ((int)(lastCard.Suit() ^ card.Suit()) & 1) != 1)
                {
                    return false;
                }

                lastCard = card;
            }
            return true;
        }

        public bool CanMove(int startRow, int startCol, int endRow, int endCol)
        {
            if (endRow < 0 || endRow > 1) throw new ArgumentOutOfRangeException("The requested end row can only be 0 or 1.");
            if (endCol < 0 || endCol >= 8) throw new ArgumentOutOfRangeException("The requested end column does not exist.");
            if (!CanMove(startRow, startCol)) return false;

            if (endRow == 0)
            {
                var endCard = (Card)_board[endCol][endRow];
                if (startRow != 0 && startRow + 1 != _board[startCol].Length) return false;
                if (endCol < 4)
                {
                    return endCard == Card.None;
                }
                else
                {
                    var startCard = (Card)_board[startCol][startRow];
                    if (endCard != Card.None)
                    {
                        return endCard.Suit() == startCard.Suit() && startCard.FaceValue() - endCard.FaceValue() == 1;
                    }
                    else
                    {
                        return startCard.FaceValue() == FaceValue.Ace;
                    }
                }
            }
            else
            {
                if (endRow >= _board[endCol].Length)
                {
                    if (startRow == 0) return true;
                    var cannotMoveRow = startRow + (MoveableStackSize >> 1);
                    return cannotMoveRow >= _board[startCol].Length;
                }
                else
                {
                    var topCard = (Card)_board[endCol][_board[endCol].Length - 1];
                    var startCard = (Card)_board[startCol][startRow];
                    return topCard.FaceValue() - startCard.FaceValue() == 1 && ((int)(startCard.Suit() ^ topCard.Suit()) & 1) == 1;
                }
            }
        }

        public bool IsSolved()
        {
            return ((Card)_board[4][0]).FaceValue() == FaceValue.King
                && ((Card)_board[5][0]).FaceValue() == FaceValue.King
                && ((Card)_board[6][0]).FaceValue() == FaceValue.King
                && ((Card)_board[7][0]).FaceValue() == FaceValue.King;
        }

        public override bool Equals(object o)
        {
            if (o is FreecellBoard b)
            {
                long topLeft1 = 0, topLeft2 = 0, topRight1 = 0, topRight2 = 0;
                for (int i = 0; i < 4; i++)
                {
                    topLeft1 |= 1L << _board[i][0];
                    topLeft2 |= 1L << b._board[i][0];
                    topRight1 |= 1L << _board[i + 4][0];
                    topRight2 |= 1L << b._board[i + 4][0];
                }
                if (topLeft1 != topLeft2 || topRight1 != topRight2) return false;

                var normalizedColumnLocations1 = NormalizedColumnLocations;
                var normalizedColumnLocations2 = b.NormalizedColumnLocations;

                for (int i = 0; i < 8; i++)
                {
                    var col1 = normalizedColumnLocations1[i];
                    var col2 = normalizedColumnLocations2[i];
                    var rowHeight = _board[col1].Length;
                    if (b._board[col2].Length != rowHeight) return false;
                    for (int row = 1; row < rowHeight; row++)
                    {
                        var card = _board[col1][row];
                        if (card != b._board[col2][row]) return false;
                    }
                }

                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash;
            var normalizedColumnLocations = NormalizedColumnLocations;

            int topLeft = 0, topRight = 0;
            for (int i = 0; i < 4; i++)
            {
                topLeft |= 1 << (_board[i][0] % 32);
                topRight |= 1 << (_board[i + 4][0] % 32);
            }
            hash = topLeft;
            hash = unchecked(hash * 61) ^ topRight;

            for (int i = 0; i < 8; i++)
            {
                var col = normalizedColumnLocations[i];
                var rowHeight = _board[col].Length;
                for (int row = 1; row < rowHeight; row++)
                {
                    var card = _board[col][row];
                    hash = unchecked(hash * 61) ^ card;
                }
            }

            return hash;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(" ", Enumerable.Range(0, 4).Select(x => this[0, x].GetDisplay().PadLeft(3) ?? "   ")));
            sb.Append("   ");
            sb.Append(string.Join(" ", Enumerable.Range(4, 4).Select(x => this[0, x].GetDisplay().PadLeft(3) ?? "   ")));
            for (int row = 1; row < 20; row++)
            {
                sb.Append("\n ");
                sb.Append(string.Join(" ", Enumerable.Range(0, 8).Select(x => this[row, x].GetDisplay().PadLeft(3) ?? "   ")));
                sb.Append(" ");
            }
            return sb.ToString();
        }
    }
}
