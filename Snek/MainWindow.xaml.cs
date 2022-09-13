#region Usings
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Media;
using System.Linq;
using System; 
#endregion

namespace Snek
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer gameTickTimer = new DispatcherTimer();
        DAL.DataStream dataStream;
        public MainWindow()
        {
            InitializeComponent();
            dataStream = new DAL.DataStream();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            gameTickTimer.Tick += GameTickTimer_Tick;
            this.DataContext = this;
            dataStream.LoadHighscoreList();
            HighScoreList = dataStream.GetHighScoreList();
        }

        #region Fields
        private Random _rnd = new Random();
        private UIElement? _snekFood = null;
        private SolidColorBrush _foodBrush = Brushes.Red;
        private SolidColorBrush _snekBodyBrush = Brushes.Green;
        private SolidColorBrush _snekHeadBrush = Brushes.DarkGreen;
        private List<SnekPart> _snekParts = new List<SnekPart>();
        private SnekDirection _snekDirection = SnekDirection.Right;
        private int _snekLength;
        private int _currentScore = 0;
        private bool _gameRunning;
        #endregion

        #region Const
        const int MaxHighscoreListEntryCount = 5;
        const int SnekSquareSize = 20;
        const int SnekStartLength = 3;
        const int SnekStartSpeed = 200;
        const int SnekSpeedThreshold = 100; 
        #endregion

        public enum SnekDirection { Left, Right, Up, Down };
        public ObservableCollection<SnekHighScore> HighScoreList { get; set; }
        
        #region Events
        private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;
            // Where should the new entry be inserted?
            if ((this.HighScoreList.Count > 0) && (_currentScore < this.HighScoreList.Max(x => x.Score)))
            {
                SnekHighScore justAbove = this.HighScoreList.OrderByDescending(x => x.Score).First(x => x.Score >= _currentScore);
                if (justAbove != null)
                    newIndex = this.HighScoreList.IndexOf(justAbove) + 1;
            }
            // Create & insert the new entry
            this.HighScoreList.Insert(newIndex, new SnekHighScore()
            {
                PlayerName = txtPlayerName.Text,
                Score = _currentScore
            });
            // Make sure that the amount of entries does not exceed the maximum
            while (this.HighScoreList.Count > MaxHighscoreListEntryCount)
                this.HighScoreList.RemoveAt(MaxHighscoreListEntryCount);

            dataStream.SaveHighscoreList();
            HighScoreList = dataStream.GetHighScoreList();

            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void Window_OnKeyClickUp(object sender, KeyEventArgs e)
        {
            SnekDirection originalsnekDirection = _snekDirection;

            switch (e.Key)
            {
                case Key.Up or Key.W:
                    if (_snekDirection != SnekDirection.Down)
                        _snekDirection = SnekDirection.Up;
                    break;
                case Key.Down or Key.S:
                    if (_snekDirection != SnekDirection.Up)
                        _snekDirection = SnekDirection.Down;
                    break;
                case Key.Left or Key.A:
                    if (_snekDirection != SnekDirection.Right)
                        _snekDirection = SnekDirection.Left;
                    break;
                case Key.Right or Key.D:
                    if (_snekDirection != SnekDirection.Left)
                        _snekDirection = SnekDirection.Right;
                    break;
                case Key.P:
                    PauseGame();
                    break;
                case Key.Space:
                    if (!txtPlayerName.IsFocused)
                    {
                        StartNewGame();
                    }
                    break;
            }

            if (_snekDirection != originalsnekDirection && gameTickTimer.IsEnabled)
                MoveSnek();
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawArena();
            //StartNewGame();
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnek();
        }
        #endregion

        #region Draw
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
        private void DrawFood()
        {
            Point foodPosition = GetNextFoodPosition();
            _snekFood = new Ellipse()
            {
                Width = SnekSquareSize,
                Height = SnekSquareSize,
                Fill = _foodBrush
            };
            Arena.Children.Add(_snekFood);
            Canvas.SetTop(_snekFood, foodPosition.Y);
            Canvas.SetLeft(_snekFood, foodPosition.X);
        }
        private void DrawSnek()
        {
            foreach (SnekPart bodyPart in _snekParts)
            {
                if (bodyPart.UiElement == null)
                {
                    bodyPart.UiElement = new Rectangle()
                    {
                        Width = SnekSquareSize,
                        Height = SnekSquareSize,
                        Fill = bodyPart.IsHead ? _snekHeadBrush : _snekBodyBrush
                    };
                    Arena.Children.Add(bodyPart.UiElement);
                    Canvas.SetTop(bodyPart.UiElement, bodyPart.Position.Y);
                    Canvas.SetLeft(bodyPart.UiElement, bodyPart.Position.X);
                }
            }
        }
        #endregion

        #region MISC
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(Arena.ActualWidth / SnekSquareSize);
            int maxY = (int)(Arena.ActualHeight / SnekSquareSize);
            int foodX = _rnd.Next(0, maxX) * SnekSquareSize;
            int foodY = _rnd.Next(0, maxY) * SnekSquareSize;

            foreach (SnekPart bodyPart in _snekParts)
            {
                if ((bodyPart.Position.X == foodX) && (bodyPart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }

            return new Point(foodX, foodY);
        }
        private void CollisionCheck()
        {
            SnekPart snekHead = _snekParts[_snekParts.Count - 1];

            if ((snekHead.Position.X == Canvas.GetLeft(_snekFood)) && (snekHead.Position.Y == Canvas.GetTop(_snekFood)))
            {
                EatFood();
                return;
            }

            if ((snekHead.Position.Y < 0) || (snekHead.Position.Y >= Arena.ActualHeight) ||
            (snekHead.Position.X < 0) || (snekHead.Position.X >= Arena.ActualWidth))
            {
                EndGame();
            }

            foreach (SnekPart snekBodyPart in _snekParts.Take(_snekParts.Count - 1))
            {
                if ((snekHead.Position.X == snekBodyPart.Position.X) && (snekHead.Position.Y == snekBodyPart.Position.Y))
                    EndGame();
            }
        }
        private void MoveSnek()
        {
            // Remove the last part of the snek, in preparation of the new part added below  
            while (_snekParts.Count >= _snekLength)
            {
                Arena.Children.Remove(_snekParts[0].UiElement);
                _snekParts.RemoveAt(0);
            }
            // Next up, we'll add a new element to the snek, which will be the (new) head  
            // Therefore, we mark all existing parts as non-head (body) elements and then  
            // we make sure that they use the body brush  
            foreach (SnekPart bodyPart in _snekParts)
            {
                (bodyPart.UiElement as Rectangle).Fill = _snekBodyBrush;
                bodyPart.IsHead = false;
            }

            // Determine in which direction to expand the snek, based on the current direction  
            SnekPart snekHead = _snekParts[_snekParts.Count - 1];
            double nextX = snekHead.Position.X;
            double nextY = snekHead.Position.Y;
            switch (_snekDirection)
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

            // Now add the new head part to our list of snek parts...  
            _snekParts.Add(new SnekPart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });
            //... and then have it drawn!  
            DrawSnek();
            // We'll get to this later...  
            CollisionCheck();
        }
        private void EatFood()
        {
            PlaySound();
            _snekLength++;
            _currentScore++;
            int timerInterval = Math.Max(SnekSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (_currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            Arena.Children.Remove(_snekFood);
            DrawFood();
            UpdateGameHeader();
        }
        private void UpdateGameHeader()
        {
            this.tbStatusScore.Text = _currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }
        #endregion

        #region Game State
        private void StartNewGame()
        {
            LoadSound("GameStartSoundEffect.wav");
            PlaySound();
            LoadSound("EatSoundEffect.wav");

            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;

            // Remove potential dead snek parts and leftover food...
            foreach (SnekPart bodyPart in _snekParts)
            {
                if (bodyPart.UiElement != null)
                    Arena.Children.Remove(bodyPart.UiElement);
            }
            _snekParts.Clear();
            if (_snekFood != null)
                Arena.Children.Remove(_snekFood);

            // Reset stuff
            _currentScore = 0;
            _snekLength = SnekStartLength;
            _snekDirection = SnekDirection.Right;
            _snekParts.Add(new SnekPart() { Position = new Point(SnekSquareSize * 5, SnekSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnekStartSpeed);

            // Draw the snek again and some new food...
            DrawSnek();
            DrawFood();

            // Update status
            UpdateGameHeader();

            // Go!        
            gameTickTimer.IsEnabled = true;
            _gameRunning = true;
        }
        private void PauseGame()
        {
            if (!txtPlayerName.IsFocused && _gameRunning)
            {
                if (pauseScreen.Visibility == Visibility.Hidden)
                {
                    pauseScreen.Visibility = Visibility.Visible;
                    gameTickTimer.IsEnabled = false;
                }
                else
                {
                    pauseScreen.Visibility = Visibility.Hidden;
                    gameTickTimer.IsEnabled = true;
                }
            }
        }
        private void EndGame()
        {
            _gameRunning = false;
            gameTickTimer.IsEnabled = false;
            bool isNewHighscore = false;
            if (_currentScore > 0)
            {
                int lowestHighscore = (this.HighScoreList.Count > 0 ? this.HighScoreList.Min(x => x.Score) : 0);
                if ((_currentScore >= lowestHighscore) || (this.HighScoreList.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }
            if (!isNewHighscore)
            {
                tbFinalScore.Text = _currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            LoadSound("GameOverSoundEffect.wav");
            PlaySound();
        } 
        #endregion

        #region Sound
        SoundPlayer player = new SoundPlayer();
        public void LoadSound(string path)
        {

            player.SoundLocation = path;
            player.Load();
        }
        public void PlaySound()
        {
            player.Play();
        } 
        #endregion
    }
}
