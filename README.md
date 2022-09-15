# Freecell
This repo is basically a collection of hobby freecell code I wrote. I started out writing a solver, but in the experimenting process some other projects grew out of it.
## Freecell.Structures
These are the core structures for the project, things like the FreecellBoard class and the Card enum.
## Freecell.Solver
This is the solver code. The core of it is a generic implementation of A* that works off an interface. An adapter class implements the interface for freecell boards. This project also contains a handful of different heuristic implementations to supply to the solver.
## Freecell.Wpf
This is a simple little freecell game implementation in WPF. I mostly wrote it so I could visualize the solver better. It has a hint key that utilizes the solver.
## Freecell.Identifier
This project was designed to interpret the screen in Microsoft Solitaire Collection. This task was surprising difficult due to how the game is rendered. There is a program file in it which is able to use this information to play games on its own. I was experimenting with OpenCV quite a bit while developing this, but I ultimately found keypoint detection and feature matching were a poor fit to the problem. So, I ended up doing some tweaked template matching.
## Freecell.Console
This is just a testing sandbox, nothing special.
