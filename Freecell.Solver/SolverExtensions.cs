using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Solver
{
    public static class SolverExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<FreecellBoard> GetNextBoards(this FreecellBoard board, bool shortCircuitObviousMoves = false)
        {
            // 1. Define some basic knowledge of the board
            var topCard = new Card[8];
            var bottomCard = new Card[8];
            var suitEndCol = new int[] { 4, 5, 6, 7 };
            var endColSuit = new Suit[] { Suit.Heart, Suit.Spade, Suit.Diamond, Suit.Club };
            // 1a. Find all the moveable (single) cards
            var finalRow = new int[8];
            for (int col = 0; col < 8; col++)
            {
                topCard[col] = board[0, col];
                for (int row = 1; row < 20; row++)
                {
                    var card = board[row, col];
                    if (card != Card.None)
                    {
                        finalRow[col] = row;
                        bottomCard[col] = card;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            // 1b. Define the end columns
            for (int i = 0; i < 4; i++)
            {
                var card = topCard[i + 4];
                if (card != Card.None )
                {
                    var actualSuit = card.Suit().Value;
                    var expectedSuit = endColSuit[i];
                    if (actualSuit != expectedSuit)
                    {
                        var actualSuitExpectedCol = suitEndCol[(int)actualSuit];
                        var expectedSuitExpectedCol = suitEndCol[(int)expectedSuit];
                        suitEndCol[(int)actualSuit] = expectedSuitExpectedCol;
                        suitEndCol[(int)expectedSuit] = actualSuitExpectedCol;
                        endColSuit[actualSuitExpectedCol - 4] = expectedSuit;
                        endColSuit[expectedSuitExpectedCol - 4] = actualSuit;
                    }
                }
            }

            // 2. Start by looking at obvious moves
            var maybeQueueLater = new List<FreecellBoard>();
            // 2a. Move card from top free cells to home
            for (int startCol = 0; startCol < 4; startCol++)
            {
                var card = topCard[startCol];
                if (card != Card.None)
                {
                    var suit = (int)card.Suit();
                    var face = card.FaceValue();
                    var endCol = suitEndCol[suit];
                    var endCard = topCard[endCol];
                    if (face - endCard.FaceValue() == 1)
                    {
                        var nextBoard = board.Move(0, startCol, 0, endCol);

                        if (shortCircuitObviousMoves)
                        {
                            var diffColorFace1 = topCard[suitEndCol[(suit + 1) % 4]].FaceValue();
                            var sameColorFace = topCard[suitEndCol[(suit + 2) % 4]].FaceValue();
                            var diffColorFace2 = topCard[suitEndCol[(suit + 3) % 4]].FaceValue();
                            if ((diffColorFace1 >= face - 1 && diffColorFace2 >= face - 1) || (diffColorFace1 >= face - 2 && diffColorFace2 >= face - 2 && sameColorFace >= face - 3))
                            {
                                yield return nextBoard; yield break;
                            }
                            else
                            {
                                maybeQueueLater.Add(nextBoard);
                            }
                        }
                        else
                        {
                            yield return nextBoard;
                        }
                    }
                }
            }
            // 2b. Move cards from stacks to home
            for (int startCol = 0; startCol < 8; startCol++)
            {
                var card = bottomCard[startCol];
                if (card == Card.None) continue;

                var suit = (int)card.Suit();
                var face = card.FaceValue();
                var endCol = suitEndCol[suit];
                var endCard = topCard[endCol];
                if (face - endCard.FaceValue() == 1)
                {
                    var nextBoard = board.Move(finalRow[startCol], startCol, 0, endCol);

                    if (shortCircuitObviousMoves)
                    {
                        var diffColorFace1 = topCard[suitEndCol[(suit + 1) % 4]].FaceValue();
                        var sameColorFace = topCard[suitEndCol[(suit + 2) % 4]].FaceValue();
                        var diffColorFace2 = topCard[suitEndCol[(suit + 3) % 4]].FaceValue();
                        if ((diffColorFace1 >= face - 1 && diffColorFace2 >= face - 1) || (diffColorFace1 >= face - 2 && diffColorFace2 >= face - 2 && sameColorFace >= face - 3))
                        {
                            yield return nextBoard; yield break;
                        }
                        else
                        {
                            maybeQueueLater.Add(nextBoard);
                        }
                    }
                    else
                    {
                        yield return nextBoard;
                    }
                }
            }
            // 2c. Queue the boards we skipped
            foreach (var nextBoard in maybeQueueLater)
            {
                yield return nextBoard;
            }

            // 3. Move cards between top row and bottom row
            var hasMovedUp = false;
            for (int topCol = 0; topCol < 4; topCol++)
            {
                var card = topCard[topCol];
                if (card != Card.None)
                {
                    // 3a. Move cards from the top to the bottom
                    for (int bottomCol = 0; bottomCol < 8; bottomCol++)
                    {
                        var endCard = bottomCard[bottomCol];
                        if (endCard == Card.None || (((byte)(card.Suit() ^ endCard.Suit()) & 1) == 1 && endCard.FaceValue() - card.FaceValue() == 1))
                        {
                            yield return board.Move(0, topCol, 1, bottomCol);
                        }
                    }
                }
                else if (!hasMovedUp)
                {
                    // 3b. Move cards from the bottom to the top
                    hasMovedUp = true;
                    for (int bottomCol = 0; bottomCol < 8; bottomCol++)
                    {
                        var bottomRow = finalRow[bottomCol];
                        if (bottomRow == 0) continue;
                        yield return board.Move(bottomRow, bottomCol, 0, topCol);
                    }
                }
            }

            // 4. Move cards across bottom row
            for (int col1 = 0; col1 < 7; col1++)
            {
                var card1 = bottomCard[col1];
                for (int col2 = col1 + 1; col2 < 8; col2++)
                {
                    var card2 = bottomCard[col2];
                    if (card1 != Card.None && card2 != Card.None)
                    {
                        if (((byte)(card1.Suit() ^ card2.Suit()) & 1) == 1)
                        {
                            var faceDiff = card1.FaceValue() - card2.FaceValue();
                            if (faceDiff == 1)
                            {
                                yield return board.Move(finalRow[col2], col2, 1, col1);
                            }
                            else if (faceDiff == 255)
                            {
                                yield return board.Move(finalRow[col1], col1, 1, col2);
                            }
                        }
                    }
                    else if (card1 == Card.None && card2 != Card.None)
                    {
                        yield return board.Move(finalRow[col2], col2, 1, col1);
                    }
                    else if (card1 != Card.None && card2 == Card.None)
                    {
                        yield return board.Move(finalRow[col1], col1, 1, col2);
                    }
                }
            }
        }
    }
}
