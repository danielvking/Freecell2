using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Freecell.Solver
{
    /// <summary>
    /// Static functions to apply the A* algorithm to implementors of the IAStarable interface.
    /// </summary>
    public static class AStarSolver
    {
        /// <summary>
        /// Executes the A* algorithm on the puzzle and returns the last instance, or default, if no solution is found.
        /// </summary>
        public static T Solve<T>(T puzzle, CancellationToken cancellationToken = default) where T : IAStarable<T>
        {
            foreach (var node in SolveHelper(puzzle))
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (node != null)
                {
                    return node.Content;
                }
            }

            return default;
        }

        /// <summary>
        /// Executes the A* algorithm on a sequence of puzzles, interleaved, and returns the first solved instance it finds, or default, if no solution is found.
        /// </summary>
        public static T SolveMultiplexed<T>(IEnumerable<T> puzzles, CancellationToken cancellationToken = default) where T : IAStarable<T>
        {
            var progress = new LinkedList<IEnumerator<Node<T>>>(puzzles.Select(x => SolveHelper(x).GetEnumerator()));

            while (progress.Count != 0)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var curr = progress.First;
                while (curr != null)
                {
                    var next = curr.Next;
                    var enumerator = curr.Value;
                    if (enumerator.MoveNext())
                    {
                        var node = enumerator.Current;
                        if (node != null)
                        {
                            return node.Content;
                        }
                    }
                    else
                    {
                        progress.Remove(curr);
                    }
                    curr = next;
                }
            }

            return default;
        }

        private static IEnumerable<Node<T>> SolveHelper<T>(T puzzle) where T : IAStarable<T>
        {
            var reachedSet = new Dictionary<T, HandleNode<T>>();
            var priorityQueue = new C5.IntervalHeap<Node<T>>();

            void TryAddQueue(T next, int moves)
            {
                if (reachedSet.TryGetValue(next, out var handleNode))
                {
                    // For less ideal heuristics, we need to check this
                    if (handleNode.Node.Moves > moves)
                    {
                        handleNode.Node = new Node<T>(next, moves);
                        if (priorityQueue.Find(handleNode.Handle, out _))
                        {
                            priorityQueue.Replace(handleNode.Handle, handleNode.Node);
                        }
                        else
                        {
                            priorityQueue.Add(ref handleNode.Handle, handleNode.Node);
                        }
                    }
                }
                else
                {
                    handleNode = new HandleNode<T>();
                    handleNode.Node = new Node<T>(next, moves);
                    priorityQueue.Add(ref handleNode.Handle, handleNode.Node);
                    reachedSet[next] = handleNode;
                }
            }

            TryAddQueue(puzzle, 0);

            var current = priorityQueue.DeleteMin();
            while (current.TotalHeuristicScore != current.Moves || !current.Content.IsSolved())
            {
                foreach (var next in current.Content.GetNextMoves())
                {
                    TryAddQueue(next, current.Moves + 1);
                }

                if (priorityQueue.IsEmpty)
                {
                    yield return null;
                    yield break;
                }

                current = priorityQueue.DeleteMin();

                yield return null;
            }

            yield return current;
        }

        #region Private Classes

        private class Node<T> : IComparable<Node<T>> where T : IAStarable<T>
        {
            public Node(T content, int moves)
            {
                Content = content;
                Moves = moves;
                TotalHeuristicScore = moves + content.HeuristicScore;
            }

            public T Content;
            public int Moves;
            public int TotalHeuristicScore;

            public int CompareTo(Node<T> other)
            {
                var compare = TotalHeuristicScore - other.TotalHeuristicScore;
                if (compare != 0) return compare;
                compare = other.Moves - Moves;
                if (compare != 0) return compare;
                if (Content is IComparable c)
                {
                    return c.CompareTo(other.Content);
                }
                return compare;
            }
        }

        private class HandleNode<T> where T : IAStarable<T>
        {
            public C5.IPriorityQueueHandle<Node<T>> Handle;
            public Node<T> Node;
        }

        #endregion
    }
}
