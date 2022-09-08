using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Navigation;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Windows.Controls;
using static Snek.MainWindow;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows;
using System.Linq;
using System.Text;
using System;

namespace Snek
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer gameTickTimer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnek();
        }

        private Random rnd = new Random();

        private UIElement? snekFood = null;
        private SolidColorBrush foodBrush = Brushes.Red;

        const int SnekSquareSize = 20;
        const int SnekStartLength = 3;
        const int SnekStartSpeed = 200;
        const int SnekSpeedThreshold = 100;


        private SolidColorBrush snekBodyBrush = Brushes.Green;
        private SolidColorBrush snekHeadBrush = Brushes.DarkGreen;
        private List<SnekPart> snekParts = new List<SnekPart>();

        public enum SnekDirection { Left, Right, Up, Down };
        private SnekDirection snekDirection = SnekDirection.Right;
        private int snekLength;

        private int currentScore = 0;


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawArena();
            //StartNewGame();
        }
        private void Window_OnArrowClickUp(object sender, KeyEventArgs e)
        {
            SnekDirection originalSnakeDirection = snekDirection;

            switch (e.Key)
            {
                case Key.Up:
                    if (snekDirection != SnekDirection.Down)
                        snekDirection = SnekDirection.Up;
                    break;
                case Key.Down:
                    if (snekDirection != SnekDirection.Up)
                        snekDirection = SnekDirection.Down;
                    break;
                case Key.Left:
                    if (snekDirection != SnekDirection.Right)
                        snekDirection = SnekDirection.Left;
                    break;
                case Key.Right:
                    if (snekDirection != SnekDirection.Left)
                        snekDirection = SnekDirection.Right;
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
            }

            if (snekDirection != originalSnakeDirection)
                MoveSnek();
        }
        private void DrawArena()
        {
            bool backgroundDrawn = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while (!backgroundDrawn)
            {
                Rectangle rectangle = new()
                {
                    Width = SnekSquareSize,
                    Height = SnekSquareSize,
                    Fill = nextIsOdd ? Brushes.YellowGreen : Brushes.OliveDrab
                };

                Arena.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnekSquareSize;
                if (nextX >= Arena.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnekSquareSize;
                    rowCounter++;
                    nextIsOdd = rowCounter % 2 != 0;
                }

                if (nextY >= Arena.ActualHeight)
                {
                    backgroundDrawn = true;
                }
            }
        }
        private void StartNewGame()
        {
            // Remove potential dead snake parts and leftover food...
            foreach (SnekPart bodyPart in snekParts)
            {
                if (bodyPart.UiElement != null)
                    Arena.Children.Remove(bodyPart.UiElement);
            }
            snekParts.Clear();
            if (snekFood != null)
                Arena.Children.Remove(snekFood);

            // Reset stuff
            currentScore = 0;
            snekLength = SnekStartLength;
            snekDirection = SnekDirection.Right;
            snekParts.Add(new SnekPart() { Position = new Point(SnekSquareSize * 5, SnekSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnekStartSpeed);

            // Draw the snake again and some new food...
            DrawSnek();
            DrawFood();

            // Update status
            UpdateGameHeader();

            // Go!        
            gameTickTimer.IsEnabled = true;
        }
        private void MoveSnek()
        {
            // Remove the last part of the snake, in preparation of the new part added below  
            while (snekParts.Count >= snekLength)
            {
                Arena.Children.Remove(snekParts[0].UiElement);
                snekParts.RemoveAt(0);
            }
            // Next up, we'll add a new element to the snake, which will be the (new) head  
            // Therefore, we mark all existing parts as non-head (body) elements and then  
            // we make sure that they use the body brush  
            foreach (SnekPart bodyPart in snekParts)
            {
                (bodyPart.UiElement as Rectangle).Fill = snekBodyBrush;
                bodyPart.IsHead = false;
            }

            // Determine in which direction to expand the snake, based on the current direction  
            SnekPart snekHead = snekParts[snekParts.Count - 1];
            double nextX = snekHead.Position.X;
            double nextY = snekHead.Position.Y;
            switch (snekDirection)
            {
                case SnekDirection.Left:
                    nextX -= SnekSquareSize;
                    break;
                case SnekDirection.Right:
                    nextX += SnekSquareSize;
                    break;
                case SnekDirection.Up:
                    nextY -= SnekSquareSize;
                    break;
                case SnekDirection.Down:
                    nextY += SnekSquareSize;
                    break;
            }

            // Now add the new head part to our list of snake parts...  
            snekParts.Add(new SnekPart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });
            //... and then have it drawn!  
            DrawSnek();
            // We'll get to this later...  
            CollisionCheck();
        }
        private void DrawSnek()
        {
            foreach (SnekPart bodyPart in snekParts)
            {
                if (bodyPart.UiElement == null)
                {
                    bodyPart.UiElement = new Rectangle()
                    {
                        Width = SnekSquareSize,
                        Height = SnekSquareSize,
                        Fill = bodyPart.IsHead ? snekHeadBrush : snekBodyBrush
                    };
                    Arena.Children.Add(bodyPart.UiElement);
                    Canvas.SetTop(bodyPart.UiElement, bodyPart.Position.Y);
                    Canvas.SetLeft(bodyPart.UiElement, bodyPart.Position.X);
                }
            }
        }
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(Arena.ActualWidth / SnekSquareSize);
            int maxY = (int)(Arena.ActualHeight / SnekSquareSize);
            int foodX = rnd.Next(0, maxX) * SnekSquareSize;
            int foodY = rnd.Next(0, maxY) * SnekSquareSize;

            foreach (SnekPart bodyPart in snekParts)
            {
                if ((bodyPart.Position.X == foodX) && (bodyPart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }

            return new Point(foodX, foodY);
        }
        private void DrawFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snekFood = new Ellipse()
            {
                Width = SnekSquareSize,
                Height = SnekSquareSize,
                Fill = foodBrush
            };
            Arena.Children.Add(snekFood);
            Canvas.SetTop(snekFood, foodPosition.Y);
            Canvas.SetLeft(snekFood, foodPosition.X);
        }
        private void CollisionCheck()
        {
            SnekPart snekHead = snekParts[snekParts.Count - 1];

            if ((snekHead.Position.X == Canvas.GetLeft(snekFood)) && (snekHead.Position.Y == Canvas.GetTop(snekFood)))
            {
                EatFood();
                return;
            }

            if ((snekHead.Position.Y < 0) || (snekHead.Position.Y >= Arena.ActualHeight) ||
            (snekHead.Position.X < 0) || (snekHead.Position.X >= Arena.ActualWidth))
            {
                EndGame();
            }

            foreach (SnekPart snekBodyPart in snekParts.Take(snekParts.Count - 1))
            {
                if ((snekHead.Position.X == snekBodyPart.Position.X) && (snekHead.Position.Y == snekBodyPart.Position.Y))
                    EndGame();
            }
        }
        private void EatFood()
        {
            snekLength++;
            currentScore++;
            int timerInterval = Math.Max(SnekSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            Arena.Children.Remove(snekFood);
            DrawFood();
            UpdateGameHeader();
        }
        private void UpdateGameHeader()
        {
            this.Title = "Snek™ - Score: " + currentScore + " - Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
        }
        private void EndGame()
        {
            gameTickTimer.IsEnabled = false;
            MessageBox.Show("Oooops, you died!\n\nTo start a new game, just press the Space bar...", "SnakeWPF");
        }
    }

    public class SnekPart
    {
        public UIElement UiElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }
    }
}
