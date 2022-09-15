using Freecell.Solver;
using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Freecell.Identifer
{
    class Program
    {
        private const int SCREEN_SCAN_MILLIS = 1000; // How long to wait between screen scans
        private const int MOVE_MILLIS = 100; // How long the mouse takes to move to a card
        private const int DRAG_MILLIS = 80; // How long the mouse takes to drag a card from one point to another
        private const int CLICK_MILLIS = 20; // How long to delay click bounce
        private const int INITIAL_WAIT_MILLIS = 250; // How long to assume each auto-move takes for the first 5
        private const int SUBSEQUENT_WAIT_MILLIS = 200; // How long to assume each auto-move takes after the first 5
        private const int HOME_MILLIS = 50; // How long to wait after right-clicking a card to send it home
        private const int MIN_SAME_SPACE_MILLIS = 350; // The minimum amount of time to wait before editing the same column twice; this will only be waited when applicable
        private const double MOVEMENT_STEEPNESS = 4.0; // Controls the curvature of mouse movement; larger values spend more time at the beginning and end points than the middle

        private const bool CLOSE_ADS = true; // Whether or not to attempt to close the presumed running ad after a period of inactivity
        private const int CLOSE_ADS_X = 1880; // The X coordinate on the screen to click, in absolute coordinates
        private const int CLOSE_ADS_Y = 40; // The Y coordinate on the screen to click, in absolute coordinates
        private const int CLOSE_ADS_WAIT_SECONDS = 45; // How long to wait before assuming an ad is running

        private const bool START_GAMES = true;
        private const int START_GAMES_X1 = 820; // The X coordinate on the screen to click the first button, in absolute coordinates
        private const int START_GAMES_Y1 = 420; // The X coordinate on the screen to click the first button, in absolute coordinates
        private const int START_GAMES_DELAY1_MILLIS = 700; // How long to spend moving the mouse to the first button
        private const int START_GAMES_X2 = 960; // The X coordinate on the screen to click the second button, in absolute coordinates
        private const int START_GAMES_Y2 = 770; // The X coordinate on the screen to click the second button, in absolute coordinates
        private const int START_GAMES_DELAY2_MILLIS = 500; // How long to spend moving the mouse to the second button
        private const int START_GAMES_X3 = 640; // The X coordinate on the screen to click the third button, in absolute coordinates
        private const int START_GAMES_Y3 = 820; // The X coordinate on the screen to click the third button, in absolute coordinates
        private const int START_GAMES_DELAY3_MILLIS = 500; // How long to spend moving the mouse to the third button
        private const int START_GAMES_X4 = 820; // The X coordinate on the screen to click the fourth button, in absolute coordinates
        private const int START_GAMES_Y4 = 780; // The X coordinate on the screen to click the fourth button, in absolute coordinates
        private const int START_GAMES_DELAY4_MILLIS = 1500; // How long to spend moving the mouse to the fourth button

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.WriteLine("The program will now be looking for new freecell boards to play.");
            Console.WriteLine("You can take the mouse back from the program by moving it.");
            Console.WriteLine("To end the program, close this window or press any key.");
            MainTask();
            Console.ReadKey();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }

        private static bool CardWouldAutoMove(FreecellBoard board, int homeColumn)
        {
            if (board.PreviousBoard?.PreviousBoard == null) return false;
            var card = board[0, homeColumn];
            var color = card.Color();
            var faceVal = card.FaceValue();
            if (faceVal <= FaceValue.Two) return faceVal >= FaceValue.Ace;
            var oHome = new List<Card>();
            oHome.Add(board[0, (homeColumn - 4 + 1) % 4 + 4]);
            oHome.Add(board[0, (homeColumn - 4 + 2) % 4 + 4]);
            oHome.Add(board[0, (homeColumn - 4 + 3) % 4 + 4]);
            return oHome.Where(x => x.Color() != color).Count(x => x.FaceValue() + 1 >= faceVal) >= 2;
        }

        private static IEnumerable<FreecellBoard> GetNextBoards(FreecellBoard board)
        {
            var nextBoards = board.GetNextBoards().ToList();
            var autoMove = nextBoards.FirstOrDefault(next => Enumerable.Range(4, 4).Any(col => next[0, col] != board[0, col] && CardWouldAutoMove(next, col)));
            if (autoMove != null)
            {
                return new[] { autoMove };
            }
            else
            {
                return nextBoards;
            }
        }

        static async void MainTask()
        {
            var lastPlayed = DateTime.Now;
            var closeAdAttempted = false;
            FreecellAStarAdapter solution = null; 
            while (true)
            {
                await Task.Delay(SCREEN_SCAN_MILLIS); // Come back in 1 second

                using var img = ScreenHelper.CaptureActiveWindow(out var freecellPos);

                if (img == null) continue;

                var fImg = FreecellImage.ReadImage(img);

                if (fImg == null)
                {
                    if (CLOSE_ADS && !closeAdAttempted && DateTime.Now.Subtract(lastPlayed).TotalSeconds > CLOSE_ADS_WAIT_SECONDS)
                    {
                        // Click the X
                        Debug.WriteLine($"Closing ad");
                        closeAdAttempted = true;
                        if (!await ClickHelper.MoveMouse(new Point(CLOSE_ADS_X, CLOSE_ADS_Y), MOVE_MILLIS)) goto endOfLoop;
                        ClickHelper.Click();
                    }
                    continue;
                }
                Debug.WriteLine($"** New board detected! **");

                // Solve the board
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

                var adapters = adapterOptions.Select(x => FreecellAStarAdapter.Create(fImg.Board, options =>
                {
                    options.GetNextMovesFunction = GetNextBoards;
                    x(options);
                }));

                using (var cts = new CancellationTokenSource())
                {
                    var solverTasks = adapters.Select(x => Task.Run(() => AStarSolver.Solve(x, cts.Token))).ToList();
                    var solvedTask = await Task.WhenAny(solverTasks);
                    cts.Cancel();
                    await Task.WhenAll(solverTasks);

                    solution = await solvedTask;
                }

                if (solution == null) continue;
                closeAdAttempted = false;

                // Get sequences of boards
                var current = solution.Board;
                var boards = new List<FreecellBoard>();
                while (current.PreviousBoard != null)
                {
                    boards.Insert(0, current);
                    current = current.PreviousBoard;
                }

                // Get sequence of moves
                var moves = new List<(FreecellBoard, Point, Point)>();
                foreach (var board in boards)
                {
                    var changed = new List<Point>();
                    for (int col = 0; col < 8; col++)
                    {
                        if (board[0, col] != board.PreviousBoard[0, col])
                        {
                            changed.Add(new Point(col, 0));
                        }
                    }
                    for (int col = 0; col < 8; col++)
                    {
                        var row = 1;
                        while (true)
                        {
                            if (board[row, col] != board.PreviousBoard[row, col])
                            {
                                changed.Add(new Point(col, row));
                                break;
                            }
                            if (board[row, col] == Card.None) break;
                            row++;
                        }
                    }

                    if (changed.Count != 2) break; // Doesn't make sense

                    var start = changed[0];
                    var end = changed[1];
                    if (!board.PreviousBoard.CanMove(start.Y, start.X, end.Y > 0 ? 1 : 0, end.X))
                    {
                        var temp = start;
                        start = end;
                        end = temp;
                        if (!board.PreviousBoard.CanMove(start.Y, start.X, end.Y > 0 ? 1 : 0, end.X)) break; // Doesn't make sense
                    }

                    moves.Add((board, start, end));
                }

                // Map the home spaces, which might not match
                var homeOrder = new[] { Suit.Heart, Suit.Club, Suit.Diamond, Suit.Spade }
                    .Select((x, i) => new { x, i })
                    .ToDictionary(x => x.x, x => x.i);
                var homeSpaces = new[] { solution.Board[0, 4], solution.Board[0, 5], solution.Board[0, 6], solution.Board[0, 7] };
                var homeMap = homeSpaces.Select(x => homeOrder[x.Suit().Value]).ToArray();

                // Keep track of the last time each stack was modified
                var lastModified = new long[2, 8];
                var autoMovePairs = new List<(Point, Point)>();
                async Task<bool> WaitMinimumTime(int x, int y)
                {
                    var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    var diff = MIN_SAME_SPACE_MILLIS - (now - lastModified[y > 0 ? 1 : 0, x]);
                    if (diff > 0)
                    {
                        Debug.WriteLine($"Waiting {diff}...");
                        return await ClickHelper.Delay((int)diff);
                    }
                    return true;
                }
                void UpdateModifedTime(int x, int y)
                {
                    var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    lastModified[y > 0 ? 1 : 0, x] = now;
                }

                // There is a bug that stacks of cards do not expand back to the correct size after auto-moves unless you click them
                var needsRefresh = new HashSet<int>();

                // Make each move
                for (int i = 0; i < moves.Count; i++) 
                {
                    var (board, start, end) = moves[i];

                    var moving = board[end.Y, end.X];

                    if (end.Y == 0 && end.X >= 4)
                    {
                        var autoMove = CardWouldAutoMove(board, end.X);

                        // Remap the top spaces
                        end.X = homeMap[end.X - 4] + 4;

                        if (autoMove)
                        {
                            Debug.WriteLine($"{moving}: auto-move");
                            if (start.Y > 10) needsRefresh.Add(start.X);
                            var wait = autoMovePairs.Count < 5 ? INITIAL_WAIT_MILLIS : SUBSEQUENT_WAIT_MILLIS;
                            if (!await ClickHelper.Delay(wait)) goto endOfLoop;
                            autoMovePairs.Add((start, end));
                            foreach (var (pairStart, pairEnd) in autoMovePairs)
                            {
                                UpdateModifedTime(pairStart.X, pairStart.Y);
                                UpdateModifedTime(pairEnd.X, pairEnd.Y);
                            }
                            continue;
                        }
                    }

                    // Refresh stacks after auto-move
                    foreach (var col in needsRefresh)
                    {
                        Debug.WriteLine($"{col}: needs refresh");
                        if (!await ClickHelper.MoveMouse(fImg.GetCardPosition(1, col, freecellPos), MOVE_MILLIS, steepness: MOVEMENT_STEEPNESS)) break;
                        await ClickHelper.Click(CLICK_MILLIS);
                    }
                    autoMovePairs.Clear();
                    needsRefresh.Clear();

                    // Execeute moves
                    if (!await ClickHelper.MoveMouse(fImg.GetCardPosition(start.Y, start.X, freecellPos), MOVE_MILLIS, steepness: MOVEMENT_STEEPNESS)) break;
                    if (!await WaitMinimumTime(start.X, start.Y)) goto endOfLoop;
                    if (end.Y != 0 || end.X < 4)
                    {
                        Debug.WriteLine($"{moving}: drag");
                        // Drag the card (double-clicking is error-prone and a little overwhelming to watch anyway)
                        ClickHelper.Hold();
                        UpdateModifedTime(start.X, start.Y);
                        if (!await ClickHelper.MoveMouse(fImg.GetCardPosition(end.Y, end.X, freecellPos), DRAG_MILLIS, steepness: MOVEMENT_STEEPNESS))
                        {
                            ClickHelper.Release();
                            goto endOfLoop;
                        }
                        if (!await WaitMinimumTime(end.X, end.Y)) goto endOfLoop;
                        ClickHelper.Release();
                    }
                    else
                    {
                        Debug.WriteLine($"{moving}: home");
                        // Right-click the card
                        if (!await ClickHelper.Delay(HOME_MILLIS)) goto endOfLoop;
                        if (!await WaitMinimumTime(end.X, end.Y)) goto endOfLoop;
                        await ClickHelper.Click(CLICK_MILLIS, ClickHelper.MouseButton.RIGHT);
                        UpdateModifedTime(start.X, start.Y);
                    }
                    UpdateModifedTime(end.X, end.Y);
                }

                if (START_GAMES)
                {
                    // Click the buttons to start a new game
                    if (!await ClickHelper.MoveMouse(new Point(START_GAMES_X1, START_GAMES_Y1), START_GAMES_DELAY1_MILLIS)) goto endOfLoop;
                    await ClickHelper.Click(CLICK_MILLIS);
                    if (!await ClickHelper.MoveMouse(new Point(START_GAMES_X2, START_GAMES_Y2), START_GAMES_DELAY2_MILLIS)) goto endOfLoop;
                    await ClickHelper.Click(CLICK_MILLIS);
                    if (!await ClickHelper.MoveMouse(new Point(START_GAMES_X3, START_GAMES_Y3), START_GAMES_DELAY3_MILLIS)) goto endOfLoop;
                    await ClickHelper.Click(CLICK_MILLIS);
                    if (!await ClickHelper.MoveMouse(new Point(START_GAMES_X4, START_GAMES_Y4), START_GAMES_DELAY4_MILLIS)) goto endOfLoop;
                    await ClickHelper.Click(CLICK_MILLIS);
                }

                endOfLoop:;
                lastPlayed = DateTime.Now;
            }
        }
    }
}
