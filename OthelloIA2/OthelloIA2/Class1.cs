namespace OthelloIA2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    // Tile states
    public enum tileState
    {
        WHITE,
        BLACK,
        EMPTY
    }
    
    public class OthelloBoard : IPlayable.IPlayable
    {
        private const int BOARDSIZE = 8;

        public tileState[,] board;     // X = Line     Y = Column
        private List<Tuple<int, int>> canMove;

        // stopwatch to measure time of each player
        // Offsets are for keeping the times when serialized
        private TimeSpan offset1;
        private TimeSpan offset2;
        private Stopwatch watch1;
        private Stopwatch watch2;

        // board weight for eval function
        private static int[,] weights = new int[,]{
                { 20, -3, 11, 08, 08, 11, -3, 20 },
                { -3, -7, -4, 01, 01, -4, -7, -3 },
                { 11, -4, 02, 02, 02, 02, -4, 11 },
                { 08, 01, 02, -3, -3, 02, 01, 08 },
                { 08, 01, 02, -3, -3, 02, 01, 08 },
                { 11, -4, 02, 02, 02, 02, -4, 11 },
                { -3, -7, -4, 01, 01, -4, -7, -3 },
                { 20, -3, 11, 08, 08, 11, -3, 20 }
            };

        // arrays for moving in the eight directions.
        private static int[] X1 = { -1, -1, 0, 1, 1, 1, 0, -1 };
        private static int[] Y1 = { 0, 1, 1, 1, 0, -1, -1, -1 };

        #region CONSTRUCTOR
        public OthelloBoard()
        {
            board = new tileState[BOARDSIZE, BOARDSIZE];
            canMove = new List<Tuple<int, int>>();

            offset1 = new TimeSpan(0);
            offset2 = new TimeSpan(0);

            watch1 = new Stopwatch();
            watch2 = new Stopwatch();

            // Make the board empty
            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    board[i, j] = tileState.EMPTY;
                }
            }


            // Setup first pieces
            board[3, 3] = tileState.WHITE;
            board[3, 4] = tileState.BLACK;
            board[4, 4] = tileState.WHITE;
            board[4, 3] = tileState.BLACK;

            // Get the possible moves (black always start)
            possibleMoves(false);
        }
        #endregion

        /*
         * AI
        */
        /// <summary>
        /// Constructor with a non-empty board
        /// </summary>
        /// <param name="b">int 2d array representing the board</param>
        public OthelloBoard(int[,] b)
        {
            board = new tileState[BOARDSIZE, BOARDSIZE];
            canMove = new List<Tuple<int, int>>();

            offset1 = new TimeSpan(0);
            offset2 = new TimeSpan(0);

            watch1 = new Stopwatch();
            watch2 = new Stopwatch();

            setBoard(b);
        }

        /// <summary>
        /// Evaluation function
        /// based on https://kartikkukreja.wordpress.com/2013/03/30/heuristic-function-for-reversiothello/
        /// The evaluation uses pieces differences, frontier pieces, position of the pieces, corner occupancy, corner closeness and mobility
        /// Pieces differences:
        /// based on our score and opponent score
        /// 
        /// Frontier pieces:
        /// The more frontier pieces (pieces wich have an empty neighbor) we have, those pieces have more chances to get taken by the opponent
        /// Stability calculation is very heavy so this is a simple replacement to get a light approximation of stability 
        /// 
        /// Pieces position:
        /// Positions of the pieces on the board. (To avoid giving strategical positions to opponent and take them if given)
        /// 
        /// Corner occupancy:
        /// Corners are very strong positions. So the score of the board takes in count who has corners.
        /// This could be put in the weight array but the corners scores would have to be way higher.
        /// 
        /// Corner closeness:
        /// To avoid giving corners to opponent we check who has pieces in the direct neighborhood of corners.
        /// 
        /// Mobility:
        /// Number of possible moves for us and the opponent so if we can make the opponent pass, the board have a better score
        /// 
        /// </summary>
        /// <param name="whiteTurn">is white playing</param>
        /// <returns>double score of the board</returns>
        private double eval(bool whiteTurn)
        {
            int myTiles;
            int oppTiles;


            tileState myColor = whiteTurn ? tileState.WHITE : tileState.BLACK;
            tileState oppColor = !whiteTurn ? tileState.WHITE : tileState.BLACK;
            
            // -----------------------------------------------------
            // Piece difference, frontier pieces and position values
            // -----------------------------------------------------
            double coinParity = 0;
            double frontierDisk = 0;

            int positionScore = 0;
            int myFrontTiles = 0;
            int oppFrontTiles = 0;

            int x;
            int y;
            
            myTiles = 0;
            oppTiles = 0;

            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    // piece number and position value
                    if (board[i, j] == myColor)
                    {
                        myTiles++;
                        positionScore += weights[i, j];
                    }
                    else if(board[i,j] == oppColor)
                    {
                        oppTiles++;
                        positionScore -= weights[i, j];
                    }

                    // frontier pieces
                    if(board[i,j] != tileState.EMPTY)
                    {
                        for(int k = 0; k < 8; k++)
                        {
                            x = i + X1[k];
                            y = j + Y1[k];
                            if(x >= 0 && x < BOARDSIZE && y >= 0 && y < BOARDSIZE && board[x,y] == tileState.EMPTY)
                            {
                                if (board[i, j] == myColor)
                                    myFrontTiles++;
                                else
                                    oppFrontTiles++;
                                break;
                            }
                        }
                    }
                }
            }

            // Final values for coinParity and frontierDisk
            if (myTiles > oppTiles)
                coinParity = (100.0 * myTiles) / (myTiles + oppTiles);
            else if (myTiles < oppTiles)
                coinParity = -(100.0 * oppTiles) / (myTiles + oppTiles);
            else
                coinParity = 0;

            if (myFrontTiles > oppFrontTiles)
                frontierDisk = -(100.0 * myFrontTiles) / (myFrontTiles + oppFrontTiles);
            else if (myFrontTiles < oppFrontTiles)
                frontierDisk = (100.0 * oppFrontTiles) / (myFrontTiles + oppFrontTiles);
            else
                frontierDisk = 0;

            // ----------------
            // Corner occupancy
            // ----------------
            double cornerOccupancy = 0;

            myTiles = 0;
            oppTiles = 0;

            // check each corners
            if (board[0, 0] == myColor)
                myTiles++;
            else if (board[0, 0] == oppColor)
                oppTiles++;
            if (board[0, 7] == myColor)
                myTiles++;
            else if (board[0, 7] == oppColor)
                oppTiles++;
            if (board[7, 0] == myColor)
                myTiles++;
            else if (board[7, 0] == oppColor)
                oppTiles++;
            if (board[7, 7] == myColor)
                myTiles++;
            else if (board[7, 7] == oppColor)
                oppTiles++;

            // final value for corner occupancy
            cornerOccupancy = 25 * (myTiles - oppTiles);

            // ----------------
            // Corner closeness
            // ----------------
            double cornerCloseness = 0;

            myTiles = 0;
            oppTiles = 0;

            // count the number of pieces close to each corner if corner is empty (to see if you give the opponent the corner)
            if (board[0, 0] == tileState.EMPTY)
            {
                if (board[0, 1] == myColor)
                    myTiles++;
                else if (board[0, 1] == oppColor)
                    oppTiles++;

                if (board[1, 1] == myColor)
                    myTiles++;
                else if (board[1, 1] == oppColor)
                    oppTiles++;

                if (board[1, 0] == myColor)
                    myTiles++;
                else if (board[1, 0] == oppColor)
                    oppTiles++;
            }

            if (board[0, 7] == tileState.EMPTY)
            {
                if (board[0, 6] == myColor)
                    myTiles++;
                else if (board[0, 6] == oppColor)
                    oppTiles++;

                if (board[1, 6] == myColor)
                    myTiles++;
                else if (board[1, 6] == oppColor)
                    oppTiles++;

                if (board[6, 0] == myColor)
                    myTiles++;
                else if (board[6, 0] == oppColor)
                    oppTiles++;
            }

            if (board[7, 0] == tileState.EMPTY)
            {
                if (board[7, 1] == myColor)
                    myTiles++;
                else if (board[7, 1] == oppColor)
                    oppTiles++;

                if (board[6, 1] == myColor)
                    myTiles++;
                else if (board[6, 1] == oppColor)
                    oppTiles++;

                if (board[6, 0] == myColor)
                    myTiles++;
                else if (board[6, 0] == oppColor)
                    oppTiles++;
            }

            if (board[7, 7] == tileState.EMPTY)
            {
                if (board[6, 7] == myColor)
                    myTiles++;
                else if (board[6, 7] == oppColor)
                    oppTiles++;

                if (board[6, 6] == myColor)
                    myTiles++;
                else if (board[6, 6] == oppColor)
                    oppTiles++;

                if (board[7, 6] == myColor)
                    myTiles++;
                else if (board[7, 6] == oppColor)
                    oppTiles++;
            }
            cornerCloseness = -12.5 * (myTiles - oppTiles);

            // --------
            // Mobility
            // --------
            double mobility = 0;

            possibleMoves(whiteTurn);
            myTiles = canMove.Count;
            possibleMoves(!whiteTurn);
            oppTiles = canMove.Count;

            if (myTiles > oppTiles)
                mobility = (100.0 * myTiles) / (myTiles + oppTiles);
            else if (myTiles < oppTiles)
                mobility = -(100.0 * oppTiles) / (myTiles + oppTiles);
            else
                mobility = 0;


            // -----------
            // Final score
            // -----------
            return (10 * coinParity) + (801.724 * cornerOccupancy) + (382.026 * cornerCloseness) + (78.922 * mobility) + (74.396 * frontierDisk) + (10 * positionScore);
        }

        /// <summary>
        /// Alphabeta search function
        /// </summary>
        /// <param name="root">board state</param>
        /// <param name="depth">depth of the search</param>
        /// <param name="minOrMax">1 = maximize / -1 = minimize </param>
        /// <param name="parentValue">parent score</param>
        /// <param name="whiteTurn">is white playing ?</param>
        /// <returns>value of board and tuple containing the best move to play</returns>
        private Tuple<double, Tuple<int, int>> alphabeta(int depth , int minOrMax , double parentValue, bool whiteTurn, bool amIWhite)
        {
            // minOrMax = 1 : maximize
            // minOrMax = -1 : minimize
            
            possibleMoves(whiteTurn);

            if (depth == 0 || canMove.Count == 0)
                return new Tuple<double, Tuple<int, int>>(eval(amIWhite), new Tuple<int, int>(-1, -1));

            double optVal = minOrMax * (-10000000);
            
            Tuple<int, int> optOp = null;

            List<Tuple<int, int>> moves = canMove.ToList();
            foreach (Tuple<int,int> op in moves)
            {
                // create new state
                OthelloBoard newBoard = new OthelloBoard(GetBoard());
                newBoard.PlayMove(op.Item1, op.Item2, whiteTurn);

                //alphabeta on the new state with depth - 1
                Tuple<double, Tuple<int, int>> result = newBoard.alphabeta(depth - 1, -minOrMax, optVal, !whiteTurn, amIWhite);
                                         
                if (result.Item1 * minOrMax > optVal * minOrMax)
                {
                    optVal = result.Item1;
                    optOp = op;

                    if (optVal * minOrMax > parentValue * minOrMax)
                    {
                        break;
                    }
                    
                }
                
            }
            return new Tuple<double, Tuple<int, int>>(optVal, optOp);       
        }

        public Tuple<int, int> GetNextMove(int[,] game, int level, bool whiteTurn)
        {
            setBoard(game);
            double score = eval(whiteTurn);
            Tuple<double, Tuple<int, int>> result = alphabeta(level, 1, score, whiteTurn, whiteTurn);
            return result.Item2;
        }

        /**
         *  END AI 
         */

        /**
         *  Functions to return players elapsed time
         */
        public TimeSpan elapsedWatch1()
        {
            TimeSpan ts = watch1.Elapsed + offset1;
            return ts;
        }

        public TimeSpan elapsedWatch2()
        {
            TimeSpan ts = watch2.Elapsed + offset2;
            return ts;
        }


        #region IPlayable

        public string GetName()
        {
            return "01_Ceschin_Magnin";
        }
        public bool IsPlayable(int column, int line, bool isWhite)
        {
            Tuple<int, int> pos = new Tuple<int, int>(column, line);
            possibleMoves(isWhite);
            return canMove.Contains(pos);
        }

        public bool PlayMove(int column, int line, bool isWhite)
        {
            int x = column;
            int y = line;

            Tuple<int, int> pos = new Tuple<int, int>(x, y);

            if (isWhite)
            {
                possibleMoves(isWhite);
                if (canMove.Contains(pos))
                {
                    board[x, y] = tileState.WHITE;
                    turnPieces(x, y, isWhite);
                    possibleMoves(!isWhite);

                    //start stopwatch of black player
                    watch2.Start();
                    watch1.Stop();

                    return true;
                }
                else
                    return false;
            }
            else
            {
                possibleMoves(isWhite);
                if (canMove.Contains(pos))
                {
                    board[x, y] = tileState.BLACK;
                    turnPieces(x, y, isWhite);
                    possibleMoves(!isWhite);

                    //start stopwatch of white player
                    watch1.Start();
                    watch2.Stop();

                    return true;
                }
                else
                    return false;
            }
        }

        public int GetBlackScore()
        {
            return calculateScore(tileState.BLACK);
        }

        public int GetWhiteScore()
        {
            return calculateScore(tileState.WHITE);
        }

        public int[,] GetBoard()
        {
            int[,] myBoard = new int[BOARDSIZE, BOARDSIZE];
            for (int i=0; i< BOARDSIZE; i++)
            {
                for (int j=0; j< BOARDSIZE; j++)
                {
                    if (this.board[i, j] == tileState.BLACK)
                        myBoard[i, j] = 1;
                    else if (this.board[i, j] == tileState.WHITE)
                        myBoard[i, j] = 0;
                    else
                        myBoard[i, j] = -1;
                }   
            }
            return myBoard;
        }

        #endregion

        /*-------------------------------------------------------
         * Class functions
         -------------------------------------------------------- */

        /// <summary>
        /// Play the move
        /// </summary>
        /// <param name="x">grid X value</param>
        /// <param name="y">grid Y value</param>
        /// <param name="isWhite">which player play</param>
        private void turnPieces(int x, int y, bool isWhite)
        {
            tileState color;
            tileState ennemyColor;

            int laX;
            int laY;
            bool wentTroughEnnemy;

            bool turnNorth = false;
            bool turnSouth = false;
            bool turnWest = false;
            bool turnEast = false;
            bool turnNorthEast = false;
            bool turnNorthWest = false;
            bool turnSouthEast = false;
            bool turnSouthWest = false;

            // Setup the colors we need to check
            if (!isWhite)
            {
                color = tileState.BLACK;
                ennemyColor = tileState.WHITE;
            }
            else
            {
                color = tileState.WHITE;
                ennemyColor = tileState.BLACK;
            }

            /*
             * CHECK WICH LINES TO TURN
             */

            //check NORTH
            laX = x - 1;
            wentTroughEnnemy = false;
            while (laX >= 0)
            {
                if (board[laX, y] == ennemyColor)
                {
                    // if its ennemy, look after
                    laX -= 1;
                    wentTroughEnnemy = true;
                }
                else if (board[laX, y] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnNorth = true;
                    break;
                }
                else
                {
                    //empty cell
                    break;
                }
            }

            // check SOUTH
            laX = x + 1;
            wentTroughEnnemy = false;
            while (laX < BOARDSIZE)
            {
                if (board[laX, y] == ennemyColor)
                {
                    // if its ennemy, look after
                    laX += 1;
                    wentTroughEnnemy = true;
                }
                else if (board[laX, y] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnSouth = true;
                    break;
                }
                else
                {
                    break;
                }
            }


            // check WEST
            laY = y - 1;
            wentTroughEnnemy = false;
            while (laY >= 0)
            {
                if (board[x, laY] == ennemyColor)
                {
                    // if its ennemy, look after
                    laY -= 1;
                    wentTroughEnnemy = true;
                }
                else if (board[x, laY] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnWest = true;
                    break;
                }
                else
                {
                    break;
                }
            }

            // check EAST
            laY = y + 1;
            wentTroughEnnemy = false;
            while (laY < BOARDSIZE)
            {
                if (board[x, laY] == ennemyColor)
                {
                    // if its ennemy, look after
                    laY += 1;
                    wentTroughEnnemy = true;
                }
                else if (board[x, laY] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnEast = true;
                    break;
                }
                else
                {
                    break;
                }
            }

            // check NORTH EAST
            laX = x - 1;
            laY = y + 1;
            wentTroughEnnemy = false;
            while (laX >= 0 && laY < BOARDSIZE)
            {
                if (board[laX, laY] == ennemyColor)
                {
                    // if its ennemy, look after
                    laX -= 1;
                    laY += 1;
                    wentTroughEnnemy = true;
                }
                else if (board[laX, laY] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnNorthEast = true;
                    break;
                }
                else
                {
                    break;
                }
            }

            // check NORTH WEST
            laX = x - 1;
            laY = y - 1;
            wentTroughEnnemy = false;
            while (laX >= 0 && laY >= 0)
            {
                if (board[laX, laY] == ennemyColor)
                {
                    // if its ennemy, look after
                    laX -= 1;
                    laY -= 1;
                    wentTroughEnnemy = true;
                }
                else if (board[laX, laY] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnNorthWest = true;
                    break;
                }
                else
                {
                    break;
                }
            }

            // check SOUTH EAST
            laX = x + 1;
            laY = y + 1;
            wentTroughEnnemy = false;
            while (laX < BOARDSIZE && laY < BOARDSIZE)
            {
                if (board[laX, laY] == ennemyColor)
                {
                    // if its ennemy, look after
                    laX += 1;
                    laY += 1;
                    wentTroughEnnemy = true;
                }
                else if (board[laX, laY] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnSouthEast = true;
                    break;
                }
                else
                {
                    break;
                }
            }

            // check SOUTH WEST
            laX = x + 1;
            laY = y - 1;
            wentTroughEnnemy = false;
            while (laX < BOARDSIZE && laY >= 0)
            {
                if (board[laX, laY] == ennemyColor)
                {
                    // if its ennemy, look after
                    laX += 1;
                    laY -= 1;
                    wentTroughEnnemy = true;
                }
                else if (board[laX, laY] == color)
                {
                    // if its our color and we eat ennemies, we gonna turn the line
                    if (wentTroughEnnemy)
                        turnSouthWest = true;
                    break;
                }
                else
                {
                    break;
                }
            }


            /*
             *  TURN PIECES 
             */
            int tempx = x;
            int tempy = y;

            if (turnNorth)
            {
                tempx = x - 1;
                while (board[tempx, y] == ennemyColor)
                {
                    board[tempx, y] = color;
                    tempx -= 1;
                }
            }
            if (turnSouth)
            {
                tempx = x + 1;
                while (board[tempx, y] == ennemyColor)
                {
                    board[tempx, y] = color;
                    tempx += 1;
                }
            }
            if (turnEast)
            {
                tempy = y + 1;
                while (board[x, tempy] == ennemyColor)
                {
                    board[x, tempy] = color;
                    tempy += 1;
                }
            }
            if (turnWest)
            {
                tempy = y - 1;
                while (board[x, tempy] == ennemyColor)
                {
                    board[x, tempy] = color;
                    tempy -= 1;
                }
            }
            if (turnNorthEast)
            {
                tempx = x - 1;
                tempy = y + 1;
                while (board[tempx, tempy] == ennemyColor)
                {
                    board[tempx, tempy] = color;
                    tempx -= 1;
                    tempy += 1;
                }
            }
            if (turnNorthWest)
            {
                tempx = x - 1;
                tempy = y - 1;
                while (board[tempx, tempy] == ennemyColor)
                {
                    board[tempx, tempy] = color;
                    tempx -= 1;
                    tempy -= 1;
                }
            }
            if (turnSouthEast)
            {
                tempx = x + 1;
                tempy = y + 1;
                while (board[tempx, tempy] == ennemyColor)
                {
                    board[tempx, tempy] = color;
                    tempx += 1;
                    tempy += 1;
                }
            }
            if (turnSouthWest)
            {
                tempx = x + 1;
                tempy = y - 1;
                while (board[tempx, tempy] == ennemyColor)
                {
                    board[tempx, tempy] = color;
                    tempx += 1;
                    tempy -= 1;
                }
            }
        }

        /// <summary>
        /// Calculate what moves are available
        /// Available moves are in canMove field
        /// </summary>
        /// <param name="isWhite">which player play</param>
        public void possibleMoves(bool isWhite)
        {
            tileState color;
            tileState ennemyColor;
            List<Tuple<int, int>> colorList = new List<Tuple<int, int>>();

            // Reset the canMove list
            canMove.Clear();

            // Setup the colors we need to check
            if (!isWhite)
            {
                color = tileState.BLACK;
                ennemyColor = tileState.WHITE;
            }
            else
            {
                color = tileState.WHITE;
                ennemyColor = tileState.BLACK;
            }

            //Get all the color pieces on board
            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    if (board[i, j] == color)
                    {
                        colorList.Add(new Tuple<int, int>(i, j));
                    }
                }
            }


            foreach (Tuple<int, int> pos in colorList)
            {
                int x = pos.Item1;
                int y = pos.Item2;
                int laX;
                int laY;
                bool wentTroughEnnemy;

                //check NORTH
                laX = x - 1;
                wentTroughEnnemy = false;
                while (laX >= 0)
                {
                    if (board[laX, y] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laX -= 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[laX, y] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(laX, y));
                        break;
                    }
                }

                // check SOUTH
                laX = x + 1;
                wentTroughEnnemy = false;
                while (laX < BOARDSIZE)
                {
                    if (board[laX, y] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laX += 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[laX, y] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(laX, y));
                        break;
                    }
                }


                // check WEST
                laY = y - 1;
                wentTroughEnnemy = false;
                while (laY >= 0)
                {
                    if (board[x, laY] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laY -= 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[x, laY] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(x, laY));
                        break;
                    }
                }

                // check EAST
                laY = y + 1;
                wentTroughEnnemy = false;
                while (laY < BOARDSIZE)
                {
                    if (board[x, laY] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laY += 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[x, laY] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(x, laY));
                        break;
                    }
                }

                // check NORTH EAST
                laX = x - 1;
                laY = y + 1;
                wentTroughEnnemy = false;
                while (laX >= 0 && laY < BOARDSIZE)
                {
                    if (board[laX, laY] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laX -= 1;
                        laY += 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[laX, laY] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(laX, laY));
                        break;
                    }
                }

                // check NORTH WEST
                laX = x - 1;
                laY = y - 1;
                wentTroughEnnemy = false;
                while (laX >= 0 && laY >= 0)
                {
                    if (board[laX, laY] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laX -= 1;
                        laY -= 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[laX, laY] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(laX, laY));
                        break;
                    }
                }

                // check SOUTH EAST
                laX = x + 1;
                laY = y + 1;
                wentTroughEnnemy = false;
                while (laX < BOARDSIZE && laY < BOARDSIZE)
                {
                    if (board[laX, laY] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laX += 1;
                        laY += 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[laX, laY] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(laX, laY));
                        break;
                    }
                }

                // check SOUTH WEST
                laX = x + 1;
                laY = y - 1;
                wentTroughEnnemy = false;
                while (laX < BOARDSIZE && laY >= 0)
                {
                    if (board[laX, laY] == ennemyColor)
                    {
                        // if its ennemy, look after
                        laX += 1;
                        laY -= 1;
                        wentTroughEnnemy = true;
                    }
                    else if (board[laX, laY] == color)
                    {
                        // if its our color, end
                        break;
                    }
                    else
                    {
                        // if its empty and we eat an enenmy, add it to canMove
                        if (wentTroughEnnemy)
                            canMove.Add(new Tuple<int, int>(laX, laY));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Count how many pieces of the color are on the board
        /// </summary>
        /// <param name="tileColor">color of the pieces</param>
        /// <returns></returns>
        private int calculateScore(tileState tileColor)
        {
            int score = 0;

            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    if (board[i, j] == tileColor)
                        score++;
                }
            }

            return score;
        }

        /*-------------------------------------------------------
         * Getters and Setters
         -------------------------------------------------------- */
        public tileState[,] getState()
        {
            return board;
        }

        public List<Tuple<int, int>> getCanMove()
        {
            return canMove;
        }

        public void setBoard(int[,] b)
        {
            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    if (b[i, j] == 1)
                        board[i, j] = tileState.BLACK;
                    else if (b[i, j] == 0)
                        board[i, j] = tileState.WHITE;
                    else
                        board[i, j] = tileState.EMPTY;
                }
            }
        }
    }

}
