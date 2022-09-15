using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Solver
{
    /// <summary>
    /// A function which assigns a heuristic weight to a board.
    /// </summary>
    /// <param name="board">The current state of the board</param>
    /// <returns>A heuristic weight</returns>
    public delegate int HeuristicFunction(FreecellBoard board);

    public static class Heuristics
    {
        /// <summary>
        /// This heuristic functions by counting cards which are stacked on smaller cards of the same suit.
        /// It is admissible and consistent. (i.e. A solution found using this heuristic is actually minimal.)
        /// </summary>
        /// <param name="board">The freecell board to analyze</param>
        /// <returns>The number of moves required to solve</returns>
        public static int BasicHeuristic(FreecellBoard board)
        {
            var sum = 0;
            for (int col = 0; col < 4; col++)
            {
                if (board[0, col] != Card.None) sum++;
            }
            for (int col = 0; col < 8; col++)
            {
                for (int row = 19; row > 0; row--)
                {
                    var card = board[row, col];
                    if (card == Card.None) continue;
                    for (int row2 = row - 1; row2 > 0; row2--)
                    {
                        var card2 = board[row2, col];
                        if (card.Suit() == card2.Suit() && card.FaceValue() > card2.FaceValue())
                        {
                            sum++;
                            break;
                        }
                    }
                    sum++;
                }
            }
            return sum;
        }

        /// <summary>
        /// This heuristic functions by counting cards which are stacked on smaller cards of any suit.
        /// It is not admissible or consistent.
        /// </summary>
        /// <param name="board">The freecell board to analyze</param>
        /// <returns>The number of moves required to solve</returns>
        public static int LooserHeuristic(FreecellBoard board)
        {
            var sum = 0;
            for (int col = 0; col < 4; col++)
            {
                if (board[0, col] != Card.None) sum++;
            }
            for (int col = 0; col < 8; col++)
            {
                for (int row = 19; row > 0; row--)
                {
                    var card = board[row, col];
                    if (card == Card.None) continue;
                    for (int row2 = row - 1; row2 > 0; row2--)
                    {
                        var card2 = board[row2, col];
                        if (card.FaceValue() > card2.FaceValue())
                        {
                            sum++;
                            break;
                        }
                    }
                    sum++;
                }
            }
            return sum;
        }

        /// <summary>
        /// This heuristic takes the looser heuristic and weights it by 4, which seems to have better performance.
        /// It is not admissible or consistent.
        /// </summary>
        /// <param name="board">The freecell board to analyze</param>
        /// <returns>The number of moves required to solve</returns>
        public static int WeightedLooserHeuristic(FreecellBoard board)
        {
            return LooserHeuristic(board) << 2;
        }

        /// <summary>
        /// This heuristic functions by computing a minimal solution given infinite free space.
        /// It is admissible and consistent. (i.e. A solution found using this heuristic is actually minimal.)
        /// </summary>
        /// <param name="board">The freecell board to analyze</param>
        /// <returns>The number of moves required to solve</returns>
        public static int AdvancedHeuristic(FreecellBoard board)
        {
            Card card;

            // Compute basic heuristic
            var basicHeuristicSum = 0;
            var basicHeuristicWeights = new bool[20, 8];
            for (int col = 0; col < 4; col++)
            {
                if (board[0, col] != Card.None) basicHeuristicSum++;
            }
            for (int col = 0; col < 8; col++)
            {
                for (int row = 19; row > 0; row--)
                {
                    card = board[row, col];
                    if (card == Card.None) continue;
                    for (int row2 = row - 1; row2 > 0; row2--)
                    {
                        var card2 = board[row2, col];
                        if (card.Suit() == card2.Suit() && card.FaceValue() > card2.FaceValue())
                        {
                            basicHeuristicSum++;
                            basicHeuristicWeights[row, col] = true;
                            break;
                        }
                    }
                    basicHeuristicSum++;
                }
            }

            // More complex algorithm
            var priorityQueue = new C5.IntervalHeap<AdvancedHeuristicNode>();

            var topRow = new byte[8];
            for (byte col = 0; col < 8; col++)
            {
                for (byte row = 1; row < 20; row++)
                {
                    card = board[row, col];
                    if (card != Card.None) topRow[col] = row;
                    else break;
                }
            }

            var freeSet = 0L;
            var homeCards = new FaceValue[4];

            card = board[0, 0];
            if (card != Card.None) freeSet |= 1L << (byte)card;
            card = board[0, 1];
            if (card != Card.None) freeSet |= 1L << (byte)card;
            card = board[0, 2];
            if (card != Card.None) freeSet |= 1L << (byte)card;
            card = board[0, 3];
            if (card != Card.None) freeSet |= 1L << (byte)card;

            card = board[0, 4];
            if (card != Card.None) homeCards[(byte)card.Suit()] = card.FaceValue();
            card = board[0, 5];
            if (card != Card.None) homeCards[(byte)card.Suit()] = card.FaceValue();
            card = board[0, 6];
            if (card != Card.None) homeCards[(byte)card.Suit()] = card.FaceValue();
            card = board[0, 7];
            if (card != Card.None) homeCards[(byte)card.Suit()] = card.FaceValue();

            var startingNode = new AdvancedHeuristicNode()
            {
                TopRow = topRow,
                FreeSet = freeSet,
                HomeCards = homeCards,
                Moves = 0,
                HeuristicScore = basicHeuristicSum
            };
            priorityQueue.Add(startingNode);

            AdvancedHeuristicNode bestBet = null;
            while (priorityQueue.Count > 0 && (bestBet = priorityQueue.DeleteMin()).HeuristicScore != bestBet.Moves)
            {
                var nextClubFlag = 1L << (byte)CardExtensions.Create(bestBet.HomeCards[(byte)Suit.Club] + 1, Suit.Club);
                var nextDiamondFlag = 1L << (byte)CardExtensions.Create(bestBet.HomeCards[(byte)Suit.Diamond] + 1, Suit.Diamond);
                var nextHeartFlag = 1L << (byte)CardExtensions.Create(bestBet.HomeCards[(byte)Suit.Heart] + 1, Suit.Heart);
                var nextSpadeFlag = 1L << (byte)CardExtensions.Create(bestBet.HomeCards[(byte)Suit.Spade] + 1, Suit.Spade);

                var nextSet = nextClubFlag | nextDiamondFlag | nextHeartFlag | nextSpadeFlag;

                // Check free space for cards to move to home
                if ((bestBet.FreeSet & nextSet) != 0)
                {
                    var nextNode = new AdvancedHeuristicNode()
                    {
                        TopRow = bestBet.TopRow,
                        FreeSet = bestBet.FreeSet & ~nextSet,
                        HomeCards = (FaceValue[])bestBet.HomeCards.Clone(),
                        Moves = bestBet.Moves,
                        HeuristicScore = bestBet.HeuristicScore
                    };

                    if ((bestBet.FreeSet & nextClubFlag) != 0)
                    {
                        nextNode.HomeCards[(byte)Suit.Club]++;
                        nextNode.Moves++;
                    }
                    if ((bestBet.FreeSet & nextDiamondFlag) != 0)
                    {
                        nextNode.HomeCards[(byte)Suit.Diamond]++;
                        nextNode.Moves++;
                    }
                    if ((bestBet.FreeSet & nextHeartFlag) != 0)
                    {
                        nextNode.HomeCards[(byte)Suit.Heart]++;
                        nextNode.Moves++;
                    }
                    if ((bestBet.FreeSet & nextSpadeFlag) != 0)
                    {
                        nextNode.HomeCards[(byte)Suit.Spade]++;
                        nextNode.Moves++;
                    }

                    priorityQueue.Add(nextNode);
                    continue; // No point trying anything else; this is already optimal
                }

                // Check the top layer of cards for known optimal plays
                for (byte col = 0; col < 8; col++)
                {
                    var row = bestBet.TopRow[col];
                    card = board[row, col];
                    if (basicHeuristicWeights[bestBet.TopRow[col], col])
                    {
                        var nextNode = new AdvancedHeuristicNode()
                        {
                            TopRow = (byte[])bestBet.TopRow.Clone(),
                            FreeSet = bestBet.FreeSet,
                            HomeCards = bestBet.HomeCards,
                            Moves = bestBet.Moves + 1,
                            HeuristicScore = bestBet.HeuristicScore
                        };

                        nextNode.TopRow[col] = (byte)(row - 1);
                        nextNode.FreeSet |= (1L << (byte)card);

                        priorityQueue.Add(nextNode);
                        goto EndOfPriorityQueueLoop; // No point trying anything else; this is already optimal
                    }
                    else if ((nextSet & (1L << (byte)card)) != 0)
                    {
                        var nextNode = new AdvancedHeuristicNode()
                        {
                            TopRow = (byte[])bestBet.TopRow.Clone(),
                            FreeSet = bestBet.FreeSet,
                            HomeCards = (FaceValue[])bestBet.HomeCards.Clone(),
                            Moves = bestBet.Moves + 1,
                            HeuristicScore = bestBet.HeuristicScore
                        };

                        nextNode.TopRow[col] = (byte)(row - 1);
                        nextNode.HomeCards[(byte)card.Suit()]++;

                        priorityQueue.Add(nextNode);
                        goto EndOfPriorityQueueLoop; // No point trying anything else; this is already optimal
                    }
                }

                // Check deck for cards to move to home
                for (byte col = 0; col < 8; col++)
                {
                    for (byte row = bestBet.TopRow[col]; row >= 1; row--)
                    {
                        card = board[row, col];
                        if ((nextSet & (1L << (byte)card)) != 0)
                        {
                            var diff = bestBet.TopRow[col] - row;
                            var nextNode = new AdvancedHeuristicNode()
                            {
                                TopRow = (byte[])bestBet.TopRow.Clone(),
                                FreeSet = bestBet.FreeSet,
                                HomeCards = (FaceValue[])bestBet.HomeCards.Clone(),
                                Moves = bestBet.Moves + diff + 1,
                                HeuristicScore = bestBet.HeuristicScore + diff
                            };

                            nextNode.TopRow[col] = (byte)(row - 1);
                            nextNode.HomeCards[(byte)card.Suit()]++;

                            for (byte row2 = (byte)(row + 1); row2 <= bestBet.TopRow[col]; row2++)
                            {
                                nextNode.FreeSet |= 1L << (byte)board[row2, col];
                                if (basicHeuristicWeights[row2, col]) nextNode.HeuristicScore--;
                            }

                            priorityQueue.Add(nextNode);
                            break; // No point trying to move a card lower in the stack; this is already optimal
                        }
                    }
                }
                EndOfPriorityQueueLoop:;
            }
            return bestBet.Moves;
        }

        private class AdvancedHeuristicNode : IComparable<AdvancedHeuristicNode>
        {
            public byte[] TopRow;
            public long FreeSet;
            public FaceValue[] HomeCards;
            public int Moves;
            public int HeuristicScore;

            public int CompareTo(AdvancedHeuristicNode other)
            {
                var compare = HeuristicScore - other.HeuristicScore;
                if (compare != 0) return compare;
                return other.Moves - Moves;
            }
        }
    }
}
