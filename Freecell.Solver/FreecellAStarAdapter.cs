using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Solver
{
    public class FreecellAStarAdapter : IAStarable<FreecellAStarAdapter>
    {
        public static FreecellAStarAdapter Create(FreecellBoard board, Action<FreecellAStarAdapterOptions> config = null)
        {
            var options = new FreecellAStarAdapterOptions();
            config?.Invoke(options);
            return new FreecellAStarAdapter(board, options);
        }

        private readonly FreecellAStarAdapterOptions _options;

        private FreecellAStarAdapter(FreecellBoard board, FreecellAStarAdapterOptions options)
        {
            _options = options;
            Board = board;
            HeuristicScore = _options.HeuristicFunction(Board);
        }

        public FreecellBoard Board { get; }

        public int HeuristicScore { get; }

        public bool Equals(FreecellAStarAdapter other) => Board.Equals(other.Board);

        public IEnumerable<FreecellAStarAdapter> GetNextMoves()
        {
            return _options.GetNextMovesFunction(Board).Select(x => new FreecellAStarAdapter(x, _options));
        }

        public int Moves()
        {
            int moves = 0;
            var board = Board;
            while ((board = board.PreviousBoard) != null)
            {
                moves++;
            }
            return moves;
        }

        public bool IsSolved() => Board.IsSolved();

        public override int GetHashCode() => Board.GetHashCode();
    }

    public class FreecellAStarAdapterOptions
    {
        public Func<FreecellBoard, int> HeuristicFunction { get; set; } = Heuristics.WeightedLooserHeuristic;
        public Func<FreecellBoard, IEnumerable<FreecellBoard>> GetNextMovesFunction { get; set; } = DefaultNextBoard;

        private static IEnumerable<FreecellBoard> DefaultNextBoard(FreecellBoard board)
        {
            return board.GetNextBoards(shortCircuitObviousMoves: true);
        }
    }
}
