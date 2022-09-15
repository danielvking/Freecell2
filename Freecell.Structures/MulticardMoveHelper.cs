using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Structures
{
    public class MulticardMoveHelper
    {
        private static readonly int[] _factorial = new[] { 1, 1, 2, 6, 24, 120, 720 };

        private static int Ncr(int n, int r)
        {
            if (r < 0 || r > n) return 0;
            return _factorial[n] / (_factorial[n - r] * _factorial[r]);
        }

        /// <summary>
        /// Returns the largest stack of cards that can be moved given the number of free spaces and free columns.
        /// </summary>
        public static int MoveableSize(int freeSpace, int freeColumns)
        {
            if (freeSpace < 0 || freeColumns < 0) throw new ArgumentException("You cannot have negative space.");
            return (freeSpace + 1) << freeColumns;
        }

        /// <summary>
        /// Returns the number of single-card moves required to moves a stack given the number of free spaces and free columns.
        /// </summary>
        public static int SingleCardMoves(int multicardSize, int freeSpace, int freeColumns)
        {
            if (multicardSize < 0 || freeSpace < 0 || freeColumns < 0) throw new ArgumentException("You cannot have negative space.");
            if (freeColumns > 6) throw new ArgumentException("You cannot have more than 6 free columns.");

            // This is kind of hard to explain
            // The minimum number of single moves to relocate a stack using free spaces and columns is found as follows...
            // 1. Select a row of Pascal's triangle from the number of free columns
            // 2. m = a2^0 + b2^1 + c2^2 + ... where (a + b + c + ...) is the number of cards and each coefficient is, at max, the respective column of Pascal's triangle
            // 3. If free space is non-zero, it actually lengthens the max coefficients by the free space multiplied by the prior column of the triangle
            //    a = nCr(c, 0) + s * nCr(c, -1); b = nCr(c, 1) + s * nCr(c, 0); c = nCr(c, 2) + s * nCr(c, 1); ...
            // I know... Trust the math
            var sum = 0;
            for (int i = 0; i <= freeColumns + 1; i++)
            {
                var min = Math.Min(multicardSize, Ncr(freeColumns, i) + freeSpace * Ncr(freeColumns, i - 1));
                sum += min << i;
                multicardSize -= min;
                if (multicardSize == 0) return sum;
            }

            return -1;
        }
    }
}
