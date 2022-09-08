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
            MoveSnake();
        }

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


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawArena();
            StartNewGame();
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
        private void MoveSnake()
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
            //DoCollisionCheck();          
        }
        private void StartNewGame()
        {
            snekLength = SnekStartLength;
            snekDirection = SnekDirection.Right;
            snekParts.Add(new SnekPart(){ Position = new Point(SnekSquareSize * 5, SnekSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnekStartSpeed);

            DrawSnek();
            gameTickTimer.IsEnabled = true;
        }
    }

    public class SnekPart
    {
        public UIElement UiElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }
    }
}
