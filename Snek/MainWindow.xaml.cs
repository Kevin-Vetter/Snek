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
        private SnekDirection _snekDirection = SnekDirection.Right;
        private SolidColorBrush _snekHeadBrush = Brushes.DarkGreen;
        private List<SnekPart> _snekParts = new List<SnekPart>();
        private SolidColorBrush _snekBodyBrush = Brushes.Green;
        private SolidColorBrush _foodBrush = Brushes.Red;
        private UIElement? _snekFood = null;
        private int _snekSpeedThreshold = 100;
        private Random _rnd = new Random();
        private int _snekStartSpeed = 200;
        private int _currentScore = 0;
        private int _squareSize = 20;
        private bool _gameRunning;
        private int _snekLength;
        #endregion

        #region Const
        const int MaxHighscoreListEntryCount = 5;
        const int SnekStartLength = 3;
        #endregion

        public enum SnekDirection { Left, Right, Up, Down };
        public ObservableCollection<SnekHighScore> HighScoreList { get; set; }

        #region Events
        /// <summary>
        /// Triggers when the close button in leaderboard is cliked
        /// </summary>
        private void BtnCloseLeaderboard_Click(object sender, RoutedEventArgs e) => bdrHighscoreList.Visibility = Visibility.Collapsed;

        /// <summary>
        /// Triggers when the clear button in leaderboard is cliked
        /// </summary>
        private void BtnClearLeaderboard_Click(object sender, RoutedEventArgs e) => dataStream.ClearHighscoreList();

        /// <summary>
        /// Triggers when the close button of the window is clicked
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Triggers when the add highscore button in new highscore is cliked
        /// </summary>
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
            gameMode.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Triggers when the leaderboard button in menu is cliked
        /// </summary>
        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Triggers when the normal button in choose difficulty is cliked
        /// </summary>
        private void BtnDifficultyNormal_Click(object sender, RoutedEventArgs e)
        {
            _snekSpeedThreshold = 100;
            _squareSize = 20;
            _snekStartSpeed = 200;
            DrawArena();
            StartNewGame();
        }

        /// <summary>
        /// Triggers every time the timer ticks
        /// </summary>
        private void GameTickTimer_Tick(object sender, EventArgs e) => MoveSnek();

        /// <summary>
        /// Triggers when the play button in menu is cliked
        /// </summary>
        private void BtnChooseGameMode_Click(object sender, RoutedEventArgs e)
        {
            menu.Visibility = Visibility.Collapsed;
            gameMode.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Triggers when the easy button in choose difficulty is cliked
        /// </summary>
        private void BtnDifficultyEasy_Click(object sender, RoutedEventArgs e)
        {
            _snekSpeedThreshold = 150;
            _squareSize = 40;
            _snekStartSpeed = 300;
            DrawArena();
            StartNewGame();
        }

        /// <summary>
        ///  Triggers when the hard button in choose difficulty is cliked
        /// </summary>
        private void BtnDifficultyHard_Click(object sender, RoutedEventArgs e)
        {
            _snekSpeedThreshold = 50;
            _squareSize = 10;
            _snekStartSpeed = 100;
            DrawArena();
            StartNewGame();
        }

        /// <summary>
        /// Triggers when the How to Play button in menu is cliked
        /// </summary>
        private void BtnShowControls_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Use WASD or The Arrow keys to control the green snek™.\nMake it eat the red apples, but be sure not to crash into the walls or the tail of the snek™!\nUse space to reset", "How to play snek™", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Triggers when you hold mouse_1 on the window
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch (InvalidOperationException)
            {

            }
        }

        /// <summary>
        /// Triggers when content has finished rendering
        /// </summary>
        private void Window_ContentRendered(object sender, EventArgs e) => DrawArena();


        /// <summary>
        /// Triggers when you let go of a button on the keyboard
        /// </summary>
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
                    if (gameTickTimer.IsEnabled)
                    {
                        StartNewGame();
                    }
                    break;
            }

            if (_snekDirection != originalsnekDirection && gameTickTimer.IsEnabled)
                MoveSnek();
        }
        #endregion

        #region Draw
        /// <summary>
        /// Draws the games play arena
        /// </summary>
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
                    Width = _squareSize,
                    Height = _squareSize,
                    Fill = nextIsOdd ? Brushes.YellowGreen : Brushes.OliveDrab
                };

                Arena.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += _squareSize;
                if (nextX >= Arena.ActualWidth)
                {
                    nextX = 0;
                    nextY += _squareSize;
                    rowCounter++;
                    nextIsOdd = rowCounter % 2 != 0;
                }

                if (nextY >= Arena.ActualHeight)
                {
                    backgroundDrawn = true;
                }
            }
        }

        /// <summary>
        /// Draws a piece of snek food
        /// </summary>
        private void DrawFood()
        {
            Point foodPosition = GetNextFoodPosition();
            _snekFood = new Ellipse()
            {
                Width = _squareSize,
                Height = _squareSize,
                Fill = _foodBrush
            };
            Arena.Children.Add(_snekFood);
            Canvas.SetTop(_snekFood, foodPosition.Y);
            Canvas.SetLeft(_snekFood, foodPosition.X);
        }

        /// <summary>
        /// Draws the snek itself
        /// </summary>
        private void DrawSnek()
        {
            foreach (SnekPart bodyPart in _snekParts)
            {
                if (bodyPart.UiElement == null)
                {
                    bodyPart.UiElement = new Rectangle()
                    {
                        Width = _squareSize,
                        Height = _squareSize,
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
        /// <summary>
        /// Calculates the next position for snek food
        /// </summary>
        /// <returns>Point for the food</returns>
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(Arena.ActualWidth / _squareSize);
            int maxY = (int)(Arena.ActualHeight / _squareSize);
            int foodX = _rnd.Next(0, maxX) * _squareSize;
            int foodY = _rnd.Next(0, maxY) * _squareSize;

            foreach (SnekPart bodyPart in _snekParts)
            {
                if ((bodyPart.Position.X == foodX) && (bodyPart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }

            return new Point(foodX, foodY);
        }

        /// <summary>
        /// Checks if the snek has hit something it should not have
        /// </summary>
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

        /// <summary>
        /// Move the snek in the direction it is facing
        /// </summary>
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
                    nextX -= _squareSize;
                    break;
                case SnekDirection.Right:
                    nextX += _squareSize;
                    break;
                case SnekDirection.Up:
                    nextY -= _squareSize;
                    break;
                case SnekDirection.Down:
                    nextY += _squareSize;
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

        /// <summary>
        /// Snek eats a piece of food
        /// </summary>
        private void EatFood()
        {
            PlaySound();
            _snekLength++;

            if (_currentScore < 8)
                _currentScore++;
            else if (_currentScore < 15)
                _currentScore += 3;
            else
                _currentScore += 6;

            int timerInterval = Math.Max(_snekSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (_currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            Arena.Children.Remove(_snekFood);
            DrawFood();
            UpdateGameHeader();
        }

        /// <summary>
        /// Updates the score and speed
        /// </summary>
        private void UpdateGameHeader()
        {
            this.tbStatusScore.Text = _currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }
        #endregion

        #region Game State
        /// <summary>
        /// Starts a new game of snek
        /// </summary>
        private void StartNewGame()
        {
            gameMode.Visibility = Visibility.Collapsed;
            LoadSound("GameStartSoundEffect.wav");
            PlaySound();
            LoadSound("EatSoundEffect.wav");

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
            _snekParts.Add(new SnekPart() { Position = new Point(_squareSize * 5, _squareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(_snekStartSpeed);

            // Draw the snek again and some new food...
            DrawSnek();
            DrawFood();

            // Update status
            UpdateGameHeader();

            // Go!        
            gameTickTimer.IsEnabled = true;
            _gameRunning = true;
        }

        /// <summary>
        /// Pauses the snek game
        /// </summary>
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

        /// <summary>
        /// Ends the snek game
        /// </summary>
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
                gameMode.Visibility = Visibility.Visible;
            }
            LoadSound("GameOverSoundEffect.wav");
            PlaySound();
        }
        #endregion

        #region Sound
        SoundPlayer player = new SoundPlayer();
        /// <summary>
        /// Loads the sound of the specifed path
        /// </summary>
        /// <param name="path"></param>
        public void LoadSound(string path)
        {
            player.SoundLocation = path;
            player.Load();
        }

        /// <summary>
        /// Plays the sound currently loaded
        /// </summary>
        public void PlaySound() => player.Play();
        #endregion

    }
}
