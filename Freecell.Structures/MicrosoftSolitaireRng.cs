using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Structures
{
    internal class MicrosoftSolitaireRng
    {
        private int state;

        public MicrosoftSolitaireRng(int seed)
        {
            state = seed;
        }

        public int Next()
        {
            return ((state = 214013 * state + 2531011) & int.MaxValue) >> 16;
        }

        public int Next(int num)
        {
            return Next() % num;
        }
    }
}
