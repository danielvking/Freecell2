using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Solver
{
    public interface IAStarable<T> : IEquatable<T> where T : IAStarable<T>
    {
        bool IsSolved();
        int HeuristicScore { get; }
        IEnumerable<T> GetNextMoves();
    }
}
