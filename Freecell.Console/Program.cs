using Freecell.Solver;
using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Freecell.Console
{
    /// <summary>
    /// Basically just a scatch-pad console app for testing solver speeds or doing other experiments
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //SearchGames.Start();

            var adapterOptions = new Action<FreecellAStarAdapterOptions>[]
            {
                options => options.HeuristicFunction = x => Heuristics.BasicHeuristic(x),
                options => options.HeuristicFunction = x => Heuristics.BasicHeuristic(x) * 4,
                options => options.HeuristicFunction = x => Heuristics.LooserHeuristic(x),
                options => options.HeuristicFunction = x => Heuristics.LooserHeuristic(x) * 2,
                options => options.HeuristicFunction = x => Heuristics.LooserHeuristic(x) * 3,
                options => options.HeuristicFunction = x => Heuristics.WeightedLooserHeuristic(x),
                options => options.HeuristicFunction = x => Heuristics.AdvancedHeuristic(x),
                options => options.HeuristicFunction = x => Heuristics.AdvancedHeuristic(x) << 2
            };

            var numAdapters = adapterOptions.Length;

            var timeSpent = new long[numAdapters];
            var solutionFound = new int[numAdapters];
            for (int i = 0; i < 1000; i++)
            {
                var board = new FreecellBoard();

                var adapters = adapterOptions.Select(options => FreecellAStarAdapter.Create(board, options)).ToArray();

                var watch = System.Diagnostics.Stopwatch.StartNew();
                using (var cts = new CancellationTokenSource())
                {
                    var tasks = adapters.Select(x => Task.Run(() => AStarSolver.Solve(x, cts.Token))).ToArray();
                    Task.WaitAny(tasks);
                    watch.Stop();
                    cts.Cancel();
                    foreach (var taskIndex in tasks.Select((task, index) => new { task, index }).Where(x => x.task.IsCompleted && x.task != null))
                    {
                        timeSpent[taskIndex.index] += watch.ElapsedMilliseconds;
                        solutionFound[taskIndex.index]++;
                    }
                }
            }
            var averageTime = Enumerable.Range(0, numAdapters).Select(x => 1.0 * timeSpent[x] / solutionFound[x]).ToArray();

            for (int i = 0; i < numAdapters; i++)
            {
                System.Console.WriteLine($"Solutions: {solutionFound[i]}; Average Time: {averageTime[i]}");
            }
            System.Console.ReadLine();
        }
    }
}
