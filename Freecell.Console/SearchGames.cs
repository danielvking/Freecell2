using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Console
{
    public static class SearchGames
    {
        public static void Start()
        {
            var cards = new[]
            {
                Card.KingClub,
            };
            foreach (var card in cards)
            {
                var result = Search(1, card);
                System.Console.WriteLine(result);
            }
            System.Console.ReadLine();
        }

        public static int Search(int start, params Card[] cards)
        {
            bool Matches(FreecellBoard board)
            {
                var row = 1;
                var col = 0;
                foreach (var card in cards)
                {
                    if (board[row, col] != card)
                    {
                        return false;
                    }

                    col++;
                    if (col == 8)
                    {
                        col = 0;
                        row++;
                    }
                }
                return true;
            }

            for (int i = start; i < 10000000; i++)
            {
                var board = new FreecellBoard(i);
                if (Matches(board)) return i;
            }

            return -1;
        }
    }
}
