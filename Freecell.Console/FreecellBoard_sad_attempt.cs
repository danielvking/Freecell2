using Freecell.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Console.Model2
{
    public class FreecellBoard
    {
        public FreecellBoard(int? seed = null)
        {
            board = new byte[20][] { new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8], new byte[8] };

            Seed = seed ?? new Random().Next(10000000);
            var rand = new MicrosoftSolitaireRng(Seed);

            var deck = new List<byte>();
            for (var faceValue = 1; faceValue <= 13; faceValue++)
            {
                deck.Add((byte)((3 << 6) | faceValue));
                deck.Add((byte)((2 << 6) | faceValue));
                deck.Add((byte)((0 << 6) | faceValue));
                deck.Add((byte)((1 << 6) | faceValue));
            }

            var row = 1;
            var col = 0;
            for (int i = 52; i > 0; i--)
            {
                var randex = rand.Next(i);
                board[row][col] = deck[randex];
                deck[randex] = deck[i - 1];
                col++;
                if (col == 8)
                {
                    row++;
                    col = 0;
                }
            }
        }

        private FreecellBoard(FreecellBoard copy)
        {
            PreviousBoard = copy;
            board = (byte[][])copy.board.Clone();
            Seed = copy.Seed;
        }

        public int Seed { get; }

        public FreecellBoard PreviousBoard { get; }

        private readonly byte[][] board;

        public Card? this[int row, int col]
        {
            get
            {
                var val = board[row][col];
                if (val == 0) return null;
                return new Card((FaceValue)(val & 0x3F), (Suit)((val >> 6) + 1));
            }
        }

        private readonly bool?[][] canMove = new bool?[20][] { new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8], new bool?[8] };

        private int? moveableStackSize;
        private int MoveableStackSize {
            get
            {
                if (!moveableStackSize.HasValue)
                {
                    int freeSpace = 0;
                    int freeStacks = 0;

                    if (this[0, 0] == null) freeSpace++;
                    if (this[0, 1] == null) freeSpace++;
                    if (this[0, 2] == null) freeSpace++;
                    if (this[0, 3] == null) freeSpace++;
                    if (this[1, 0] == null) freeStacks++;
                    if (this[1, 1] == null) freeStacks++;
                    if (this[1, 2] == null) freeStacks++;
                    if (this[1, 3] == null) freeStacks++;
                    if (this[1, 4] == null) freeStacks++;
                    if (this[1, 5] == null) freeStacks++;
                    if (this[1, 6] == null) freeStacks++;
                    if (this[1, 7] == null) freeStacks++;

                    moveableStackSize = (freeSpace + 1) << freeStacks;
                }
                return moveableStackSize.Value;
            }
        }

        public FreecellBoard Move(int startRow, int startCol, int endRow, int endCol)
        {
            if (!CanMove(startRow, startCol, endRow, endCol)) throw new InvalidOperationException($"The requested move is invalid.");

            var copy = new FreecellBoard(this);

            if (endRow != 0)
            {
                while (this[endRow, endCol].HasValue) endRow++;
            }

            if (endRow == 0 || startRow == 0)
            {
                copy.board[endRow][endCol] = board[startRow][startCol];
                copy.board[startRow][startCol] = 0;
            }
            else
            {
                while (startRow < 20 && this[startRow, startCol].HasValue)
                {
                    copy.board[endRow++][endCol] = board[startRow][startCol];
                    copy.board[startRow++][startCol] = 0;
                }
            }

            return copy;
        }

        public bool CanMove(int row, int col)
        {
            bool? computed;
            try
            {
                computed = canMove[row][col];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException($"The requested position ({row}, {col}) does not exist.");
            }

            if (computed.HasValue) return computed.Value;

            if (row == 0) return (bool)(canMove[row][col] = (col < 4 && this[row, col].HasValue));

            var cannotMoveRow = row + MoveableStackSize;
            if (cannotMoveRow < 20 && this[cannotMoveRow, col].HasValue) return (bool)(canMove[row][col] = false);

            var lastCard = this[row, col];
            if (!lastCard.HasValue) return (bool)(canMove[row][col] = false);
            for (int r = row + 1; r < 20; r++)
            {
                var card = this[r, col];

                if (!card.HasValue) break;
                if (lastCard.Value.FaceValue - card.Value.FaceValue != 1 || ((int)(lastCard.Value.Suit ^ card.Value.Suit) & 1) != 1)
                {
                    return (bool)(canMove[row][col] = false);
                }

                lastCard = card;
            }
            return (bool)(canMove[row][col] = true);
        }

        public bool CanMove(int startRow, int startCol, int endRow, int endCol)
        {
            if (endRow < 0 || endRow > 1) throw new ArgumentOutOfRangeException("The requested end row can only be 0 or 1.");
            if (endCol < 0 || endCol >= 8) throw new ArgumentOutOfRangeException("The requested end column does not exist.");
            if (!CanMove(startRow, startCol)) return false;

            var endCard = this[endRow, endCol];
            if (endRow == 0)
            {
                if (startRow != 0 && startRow + 1 < 20 && this[startRow + 1, startCol].HasValue) return false;
                if (endCol < 4)
                {
                    return !endCard.HasValue;
                }
                else
                {
                    var startCard = this[startRow, startCol];
                    if (endCard.HasValue)
                    {
                        return endCard.Value.Suit == startCard.Value.Suit && startCard.Value.FaceValue - endCard.Value.FaceValue == 1;
                    }
                    else
                    {
                        return startCard.Value.FaceValue == FaceValue.Ace;
                    }
                }
            }
            else
            {
                if (!endCard.HasValue)
                {
                    if (startRow == 0) return true;
                    var cannotMoveRow = startRow + (MoveableStackSize >> 1);
                    return cannotMoveRow >= 20 || !this[cannotMoveRow, startCol].HasValue;
                }
                else
                {
                    var topCard = endCard;
                    for (int r = endRow + 1; r < 20; r++)
                    {
                        var temp = this[r, endCol];
                        if (temp.HasValue) topCard = temp;
                        else break;
                    }
                    var startCard = this[startRow, startCol];
                    return topCard.Value.FaceValue - startCard.Value.FaceValue == 1 && ((int)(startCard.Value.Suit ^ topCard.Value.Suit) & 1) == 1;
                }
            }
        }

        public override bool Equals(object o)
        {
            if (o is FreecellBoard b)
            {
                var normalizedColumnLocations1 = NormalizedColumnLocations();
                var normalizedColumnLocations2 = b.NormalizedColumnLocations();

                for (int i = 0; i < 8; i++)
                {
                    var col1 = normalizedColumnLocations1[0, i];
                    var col2 = normalizedColumnLocations2[0, i];
                    if (board[0][col1] != b.board[0][col2]) return false;
                    for (int row = 1; row < 20; row++)
                    {
                        col1 = normalizedColumnLocations1[1, i];
                        col2 = normalizedColumnLocations2[1, i];
                        var card = board[row][col1];
                        if (board[row][col1] != board[row][col2]) return false;
                        if (card == 0) break;
                    }
                }

                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var normalizedColumnLocations = NormalizedColumnLocations();

            int hash = 0;
            for (int i = 0; i < 8; i++)
            {
                var col = normalizedColumnLocations[0, i];
                hash = unchecked(hash * 61) ^ board[0][col];
                for (int row = 1; row < 20; row++)
                {
                    col = normalizedColumnLocations[1, i];
                    var card = board[row][col];
                    if (card == 0) break;
                    hash = unchecked(hash * 61) ^ card;
                }
            }

            return hash;
        }

        private byte[,] NormalizedColumnLocations()
        {
            var normalizedColumnLocations = new byte[,] { { 0, 1, 2, 3, 4, 5, 6, 7 }, { 0, 1, 2, 3, 4, 5, 6, 7 } };

            // Insertion sort first 4
            for (byte i = 1; i < 4; i++)
            {
                var card = board[0][i];
                for (byte j = i; j > 0; j--)
                {
                    if (card >= board[0][normalizedColumnLocations[0, j - 1]])
                    {
                        normalizedColumnLocations[0, j] = i;
                        break;
                    }
                    normalizedColumnLocations[0, j] = normalizedColumnLocations[0, j - 1];
                }
            }

            // Insertion sort second 4
            for (byte i = 5; i < 8; i++)
            {
                var card = board[0][i];
                for (byte j = i; j > 4; j--)
                {
                    if (card >= board[0][normalizedColumnLocations[0, j - 1]])
                    {
                        normalizedColumnLocations[0, j] = i;
                        break;
                    }
                    normalizedColumnLocations[0, j] = normalizedColumnLocations[0, j - 1];
                }
            }

            // Insertion sort second row
            for (byte i = 1; i < 8; i++)
            {
                var card = board[1][i];
                for (byte j = i; j > 0; j--)
                {
                    if (card >= board[1][normalizedColumnLocations[1, j - 1]])
                    {
                        normalizedColumnLocations[1, j] = i;
                        break;
                    }
                    normalizedColumnLocations[1, j] = normalizedColumnLocations[1, j - 1];
                }
            }

            return normalizedColumnLocations;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(" ", Enumerable.Range(0, 4).Select(x => this[0, x]?.ToString().PadLeft(3) ?? "   ")));
            sb.Append("   ");
            sb.Append(string.Join(" ", Enumerable.Range(4, 4).Select(x => this[0, x]?.ToString().PadLeft(3) ?? "   ")));
            for (int row = 1; row < 20; row++)
            {
                sb.Append("\n ");
                sb.Append(string.Join(" ", Enumerable.Range(0, 8).Select(x => this[row, x]?.ToString().PadLeft(3) ?? "   ")));
                sb.Append(" ");
            }
            return sb.ToString();
        }
    }
}
