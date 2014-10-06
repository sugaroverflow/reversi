using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace _flipgame
{
    public class Board
    {
        public bool[] boardState;
        public HashSet<int> steps = new HashSet<int>();

        private int boardSize;

        public Board(int length)
        {
            boardSize = length;
            boardState = new bool[length * length];
        }

        //Flip a node and those adjacent to it
        public void flip(int nodePos)
        {
            boardState[nodePos] = !boardState[nodePos];

            //Flip adjacent Nodes
            if (nodePos - 4 >= 0)
                boardState[nodePos - 4] = !boardState[nodePos - 4];
            if (nodePos + 4 < boardSize * boardSize)
                boardState[nodePos + 4] = !boardState[nodePos + 4];
            if (nodePos - 1 >= 0 && (nodePos - 1) % 4 != 3)
                boardState[nodePos - 1] = !boardState[nodePos - 1];
            if (nodePos + 1 < boardSize * boardSize && (nodePos + 4) % 4 != 3)
                boardState[nodePos + 1] = !boardState[nodePos + 1];
        }

        //Returns new Board object with same contents
        public Board getCopy()
        {
            Board b = new Board(boardSize);

            //copy board nodes
            for (int i = 0; i < boardSize * boardSize; i++)
            {
                b.boardState[i] = boardState[i];
            }

            //copy board steps
            b.steps = new HashSet<int>(steps);

            return b;
        }

    }

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //const values
        //Board size
        const int MAX_LENGTH = 4;

        //Random object
        Random rand = new Random();

        //Textures
        Texture2D black;
        Texture2D white;
        Texture2D hint;

        //Buttons
        Rectangle hintRect;
        Texture2D hintSprite;
        int hintPos = -1;

        Rectangle newGameRect;
        Texture2D newGameSprite;

        //Game variables
        Board board = new Board(MAX_LENGTH);
        HashSet<int> solution = new HashSet<int>();
        bool winner = false;

        MouseState prevMouseState = Mouse.GetState();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //Set size of window
            graphics.PreferredBackBufferWidth = 400;
            graphics.PreferredBackBufferHeight = 500;

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            //Populate game board
            for (int i = 0; i < MAX_LENGTH * MAX_LENGTH; i++)
            {
                board.boardState[i] = true;
            }

            //Put game into random winnable state
            int numMoves = 5;
            int prevNode = -1;
            for (int i = 0; i < numMoves; i++)
            {
                int randNode = rand.Next(0, MAX_LENGTH * MAX_LENGTH - 1);
                if (randNode != prevNode)
                {
                    board.flip(randNode);
                    prevNode = randNode;
                }
            }

            //Calculate solution
            calculateSolution();

            //Button rectangles
            hintRect = new Rectangle(210, 450, 116, 36);
            newGameRect = new Rectangle(70, 450, 116, 36);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //Load sprites
            black = Content.Load<Texture2D>(@"sprites\blacircle2");
            white = Content.Load<Texture2D>(@"sprites\whicircle2");
            hint = Content.Load<Texture2D>(@"sprites\whicircle2");
            hintSprite = Content.Load<Texture2D>(@"sprites\hintblock");
            newGameSprite = Content.Load<Texture2D>(@"sprites\resetblock");
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            //Check mouse input
            MouseState currentMouseState = Mouse.GetState();
            if (IsActive && prevMouseState.LeftButton != ButtonState.Pressed && currentMouseState.LeftButton == ButtonState.Pressed)
            {
                Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);

                //Show hint
                if (hintRect.Contains(mousePoint) && !winner)
                {
                    hintPos = solution.First();
                }

                //New game
                if (newGameRect.Contains(mousePoint))
                {
                    newGame();
                }

                if (currentMouseState.X < 400 && currentMouseState.X > 0 && currentMouseState.Y < 400 && currentMouseState.Y > 0 && !winner)
                {
                    //Calculate cell being clicked
                    int nodePos = (int)(Math.Floor((double)(currentMouseState.X / 100)) + Math.Floor((double)(currentMouseState.Y / 100)) * 4);

                    board.flip(nodePos);

                    //Check if game is won
                    if (isGoal(board))
                    {
                        winner = true;
                        hintPos = -1;
                    }
                    else
                    {
                        //Add to solution if player made unoptimal move
                        if (!solution.Contains(nodePos))
                        {
                            hintPos = -1;
                            solution.Add(nodePos);
                        }
                        else
                        {
                            //Remove from queue and remove hint
                            solution.Remove(nodePos);
                            hintPos = -1;
                        }
                    }
                }
            }
            prevMouseState = currentMouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.LightSlateGray);

            spriteBatch.Begin();

            //Calculate positions for drawing
            int x = 0;
            int y = 0;
            for (int i = 0; i < MAX_LENGTH * MAX_LENGTH; i++)
            {
                if (i != 0 && i % 4 == 0)
                {
                    x = 0;
                    y++;
                }

                if (board.boardState[i])
                {
                    if (i == hintPos)
                        spriteBatch.Draw(hint, new Vector2(x * 100, y * 100), Color.Pink);
                    else
                        spriteBatch.Draw(black, new Vector2(x * 100, y * 100), Color.White);
                }
                else
                {
                    if (i == hintPos)
                        spriteBatch.Draw(hint, new Vector2(x * 100, y * 100), Color.Pink);
                    else
                        spriteBatch.Draw(white, new Vector2(x * 100, y * 100), Color.White);
                }
                x++;
            }

            //Draw buttons
            spriteBatch.Draw(hintSprite, hintRect, Color.White);
            spriteBatch.Draw(newGameSprite, newGameRect, Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        //Start a new game
        public void newGame()
        {
            //Put game into random winnable state
            int numMoves = 5;
            int prevNode = -1;
            for (int i = 0; i < numMoves; i++)
            {
                int randNode = rand.Next(0, MAX_LENGTH * MAX_LENGTH - 1);
                if (randNode != prevNode)
                {
                    board.flip(randNode);
                    prevNode = randNode;
                }
            }

            calculateSolution();

            winner = false;
        }

        //Checks if a boardState is a goal state (all black or all white)
        public bool isGoal(Board testBoard)
        {
            int blackCount = 0;
            int whiteCount = 0;

            for (int i = 0; i < MAX_LENGTH * MAX_LENGTH; i++)
            {
                if (testBoard.boardState[i])
                    blackCount++;
                else
                    whiteCount++;
            }
            if (blackCount == MAX_LENGTH * MAX_LENGTH || whiteCount == MAX_LENGTH * MAX_LENGTH)
                return true;
            else
                return false;
        }

        //Calculate the solution and populate it into solutions queue
        //Breadth First Search
        public void calculateSolution()
        {
            Queue<Board> boardQueue = new Queue<Board>();
            HashSet<Board> visited = new HashSet<Board>();
            Board boardCopy = board.getCopy();

            boardQueue.Enqueue(boardCopy);

            while (boardQueue.Count != 0)
            {
                Board current = boardQueue.Dequeue();
                if (!isGoal(current))
                {
                    if (!visited.Contains(current))
                    {
                        for (int i = 0; i < MAX_LENGTH * MAX_LENGTH; i++)
                        {
                            Board temp = current.getCopy();
                            temp.flip(i);
                            temp.steps.Add(i);
                            boardQueue.Enqueue(temp);
                        }
                        visited.Add(current);
                    }
                }
                else
                {
                    solution = new HashSet<int>(current.steps);
                    boardQueue.Clear();
                }
            }
        }
    }
}