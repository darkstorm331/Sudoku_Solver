using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DrWPF.Windows.Data;

namespace Sudoku_Solver.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private ObservableDictionary<string, int?> _theBoard;
        public ObservableDictionary<string, int?> TheBoard
        {
            get 
            { 
                return _theBoard; 
            }
            set
            {
                _theBoard = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        //Command Binding
        private ICommand _resetCommand;
        private ICommand _solveCommand;

        public ICommand ResetCommand
        {
            get
            {
                return _resetCommand;
            }
            set
            {
                _resetCommand = value;
            }
        }

        public ICommand SolveCommand
        {
            get
            {
                return _solveCommand;
            }
            set
            {
                _solveCommand = value;
            }
        }

        public MainViewModel()
        {
            //Setup board          
            TheBoard = new ObservableDictionary<string, int?>();

            char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I' };

            foreach (var letter in letters)
            {
                for (int i = 1; i <= 9; i++)
                {
                    TheBoard.Add($"{letter}{i}", null);
                }
            }

            //Bind Commands
            ResetCommand = new RelayCommand(new Action<object>(Reset));
            SolveCommand = new RelayCommand(new Action<object>(Solve));

            //Set loading to false
            IsLoading = false;
        }

        private async void Reset(object obj)
        {
            if(!IsLoading)
            {
                Console.WriteLine("Reset");
                TheBoard.Keys.ToList().ForEach(k => TheBoard[k] = null);
            }
        }

        private async void Solve(object obj)
        {
            Random rnd = new Random();
            IsLoading = true;

            var task = Task.Run(() => 
            {
                bool solved = false;

                ObservableDictionary<string, int?> originalBoardState = new ObservableDictionary<string, int?>(TheBoard);

                //Get the tiles without numbers in them
                List<string> emptyTiles = new List<string>();

                int possibilityCount = 1;
                int tries = 0;
                int restarts = 0;
                do
                {
                    Console.WriteLine($"Looping. Possibility count of {possibilityCount}");
                    //Get new list of empty tiles
                    emptyTiles.Clear();
                    foreach (KeyValuePair<string, int?> cell in TheBoard)
                    {
                        if (cell.Value == null)
                        {
                            emptyTiles.Add(cell.Key);
                        }
                    }

                    int emptyCount = emptyTiles.Count;
                    
                    //See if there is an empty tile with only one possible value
                    foreach (string cell in emptyTiles)
                    {
                        //To get the individual parts of the cell
                        char[] splitCell = cell.ToCharArray();

                        List<int> possibleNums = RowValidVals(int.Parse(splitCell[1].ToString()));
                        possibleNums.AddRange(ColValidVals(splitCell[0].ToString(), possibleNums).Where(a => !possibleNums.Contains(a)));
                        possibleNums.AddRange(QuadrantValidVals(GetQuadrant(cell), possibleNums).Where(a => !possibleNums.Contains(a)));

                        if (possibleNums.Count <= possibilityCount && possibleNums.Count != 0)
                        {
                            //Can put in a value;
                            TheBoard[cell] = possibleNums.ElementAt(rnd.Next(0, possibleNums.Count));
                            tries = 0;
                            possibilityCount = 1;
                            Console.WriteLine($"Adding {possibleNums.ElementAt(0)} to {cell}");
                        }
                        else if (tries == emptyCount && possibilityCount < 9)
                        {
                            Console.WriteLine("No Options");

                            possibilityCount++;                     
                            break;
                        } else if(possibilityCount >= 9 && restarts < 1000000)
                        {
                            //Try again
                            TheBoard = new ObservableDictionary<string, int?>(originalBoardState);
                            restarts++;
                            break;
                        }                    
                        else if (restarts >= 1000000) //Give up
                        {
                            MessageBox.Show("Couldn't solve", "Couldn't solve", MessageBoxButton.OK, MessageBoxImage.Error);
                            solved = true;
                            break;
                        }
                        else
                        {
                            tries++;
                            continue;
                        }
                    }

                    var nullCount = TheBoard.Where(a => a.Value == null);

                    if (nullCount.Count() == 0)
                    {
                        MessageBox.Show("Solved", "Solved", MessageBoxButton.OK, MessageBoxImage.Information);
                        solved = true;
                    }
                } while (!solved);
            });

            await task;

            IsLoading = false;
        }


        private List<int> RowValidVals(int row)
        {
            List<int> output = new List<int>() { 1,2,3,4,5,6,7,8,9 };

            char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I' };
            foreach(char letter in letters)
            {
                if(TheBoard[$"{letter}{row}"] != null)
                {
                    output.Remove(TheBoard[$"{letter}{row}"] ?? default(int));
                }
            }

            return output;          
        }

        private List<int> ColValidVals(string col, List<int> starting)
        {
            List<int> output = starting;

            for(int i = 1; i <= 9; i++)
            {
                if(TheBoard[$"{col}{i}"] != null)
                {
                    output.Remove(TheBoard[$"{col}{i}"] ?? default(int));
                }
            }

            return output;
        }

        private List<int> QuadrantValidVals(int quadrant, List<int> starting)
        {
            var quadrantItems = GetQuadrantList().Where(a => a.Value == quadrant);
            List<int> output = starting;

            foreach(KeyValuePair<string, int> val in quadrantItems)
            {
                if(TheBoard[val.Key] != null)
                {
                    output.Remove(TheBoard[val.Key] ?? default(int));
                }
            }

            return output;
        }

        private int GetQuadrant(string cell)
        {
            char[] splitCell = cell.ToCharArray();
            char col = splitCell[0];
            int row = int.Parse(splitCell[1].ToString());
        
            if(col == 'A' || col == 'B' || col == 'C')
            {
                if(row <= 3)
                {
                    return 1;
                } else if(row > 3 && row <= 6)
                {
                    return 4;
                } else
                {
                    return 7;
                }
            } else if(col == 'D' || col == 'E' || col == 'F')
            {
                if (row <= 3)
                {
                    return 2;
                }
                else if (row > 3 && row <= 6)
                {
                    return 5;
                }
                else
                {
                    return 8;
                }
            } else
            {
                if (row <= 3)
                {
                    return 3;
                }
                else if (row > 3 && row <= 6)
                {
                    return 6;
                }
                else
                {
                    return 9;
                }
            }
        }


        private Dictionary<string, int> GetQuadrantList()
        {
            Dictionary<string, int> output = new Dictionary<string, int>();

            foreach(KeyValuePair<string, int?> val in TheBoard)
            {
                output.Add(val.Key, GetQuadrant(val.Key));
            }

            return output;
        }
    }
}
