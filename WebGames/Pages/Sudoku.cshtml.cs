using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WebGames.Pages
{
    public class SudokuModel : PageModel
    {
        private readonly IMemoryCache _memoryCache;

        public SudokuModel(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public enum DifficultyLevel
        {
            Testing,
            VeryEasy,
            Easy,
            Medium,
            Hard,
            VeryHard,
            Brutal
        }

        private static readonly Dictionary<DifficultyLevel, int> DifficultyCellCount = new Dictionary<DifficultyLevel, int>
        {
            { DifficultyLevel.Testing, 81 },
            { DifficultyLevel.VeryEasy, 30 },
            { DifficultyLevel.Easy, 25 },
            { DifficultyLevel.Medium, 20 },
            { DifficultyLevel.Hard, 15 },
            { DifficultyLevel.VeryHard, 10 },
            { DifficultyLevel.Brutal, 5 }
        };

        public int GridSize { get; } = 9;
        public int SquareSize { get; } = 3;

        [BindProperty]
        public DifficultyLevel SelectedDifficulty { get; set; }

        public int[,] Grid
        {
            get => _memoryCache.Get<int[,]>("SudokuGrid");
            set => _memoryCache.Set("SudokuGrid", value);
        }

        public int[,] CorrectGrid
        {
            get => _memoryCache.Get<int[,]>("SudokuCorrectGrid");
            set => _memoryCache.Set("SudokuCorrectGrid", value);
        }

        public int[,] UserGrid
        {
            get => _memoryCache.Get<int[,]>("SudokuUserGrid");
            set => _memoryCache.Set("SudokuUserGrid", value);
        }

        public string[,] NotesGrid
        {
            get => _memoryCache.Get<string[,]>("SudokuNotesGrid");
            set => _memoryCache.Set("SudokuNotesGrid", value);
        }

        public void OnGet()
        {
            if (!_memoryCache.TryGetValue("SudokuGrid", out int[,] grid))
            {
                SelectedDifficulty = DifficultyLevel.Medium;
                grid = GenerateRandomSudokuBoard(SelectedDifficulty);
                _memoryCache.Set("SudokuGrid", grid);
            }
        }

        public void OnPostChangedifficulty()
        {
            var grid = GenerateRandomSudokuBoard(SelectedDifficulty);
            _memoryCache.Set("SudokuGrid", grid);
        }


        public int[,] GenerateRandomSudokuBoard(DifficultyLevel difficulty)
        {
            var grid = new int[GridSize, GridSize];
            _memoryCache.Set("SudokuUserGrid", grid);
            var notesGrid = new string[GridSize, GridSize];
            _memoryCache.Set("SudokuNotesGrid", notesGrid);
            var random = new Random();

            // Generate a complete Sudoku grid
            SolveSudoku(grid, random);

            // Copy the complete grid to the correct grid
            var correctGrid = new int[GridSize, GridSize];
            Array.Copy(grid, correctGrid, GridSize * GridSize);
            _memoryCache.Set("SudokuCorrectGrid", correctGrid);

            // Remove cells based on the selected difficulty level
            var cellCount = DifficultyCellCount[difficulty];
            RemoveCells(grid, cellCount, random);

            return grid;
        }

        private bool SolveSudoku(int[,] grid, Random random)
        {
            var emptyCells = GetEmptyCells(grid);

            if (emptyCells.Count == 0)
                return true;

            var rowIndex = emptyCells[0].Item1;
            var colIndex = emptyCells[0].Item2;

            var numbers = Enumerable.Range(1, GridSize).ToList();
            numbers = numbers.OrderBy(x => random.Next()).ToList();

            foreach (var number in numbers)
            {
                if (IsValidPlacement(grid, rowIndex, colIndex, number))
                {
                    grid[rowIndex, colIndex] = number;

                    if (SolveSudoku(grid, random))
                        return true;

                    grid[rowIndex, colIndex] = 0;
                }
            }

            return false;
        }

        private List<(int, int)> GetEmptyCells(int[,] grid)
        {
            var emptyCells = new List<(int, int)>();

            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (grid[row, col] == 0)
                        emptyCells.Add((row, col));
                }
            }

            return emptyCells;
        }

        private bool IsValidPlacement(int[,] grid, int rowIndex, int colIndex, int number)
        {
            return IsRowValid(grid, rowIndex, number)
                && IsColumnValid(grid, colIndex, number)
                && IsSquareValid(grid, rowIndex, colIndex, number);
        }

        private bool IsRowValid(int[,] grid, int rowIndex, int number)
        {
            for (int col = 0; col < GridSize; col++)
            {
                if (grid[rowIndex, col] == number)
                    return false;
            }

            return true;
        }

        private bool IsColumnValid(int[,] grid, int colIndex, int number)
        {
            for (int row = 0; row < GridSize; row++)
            {
                if (grid[row, colIndex] == number)
                    return false;
            }

            return true;
        }

        private bool IsSquareValid(int[,] grid, int rowIndex, int colIndex, int number)
        {
            var squareRowStart = rowIndex - rowIndex % SquareSize;
            var squareColStart = colIndex - colIndex % SquareSize;

            for (int row = squareRowStart; row < squareRowStart + SquareSize; row++)
            {
                for (int col = squareColStart; col < squareColStart + SquareSize; col++)
                {
                    if (grid[row, col] == number)
                        return false;
                }
            }

            return true;
        }

        private void RemoveCells(int[,] grid, int cellCount, Random random)
        {
            var cellsToRemove = GridSize * GridSize - cellCount;

            while (cellsToRemove > 0)
            {
                var rowIndex = random.Next(GridSize);
                var colIndex = random.Next(GridSize);

                if (grid[rowIndex, colIndex] != 0)
                {
                    grid[rowIndex, colIndex] = 0;
                    cellsToRemove--;
                }
            }
        }

        public string ConvertToJaggedArrayJson(int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            int[][] jaggedArray = new int[rows][];

            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new int[cols];
                for (int j = 0; j < cols; j++)
                {
                    jaggedArray[i][j] = array[i, j];
                }
            }

            return JsonSerializer.Serialize(jaggedArray);
        }

        public string ConvertToJaggedArrayJson(string[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            string[][] jaggedArray = new string[rows][];

            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new string[cols];
                for (int j = 0; j < cols; j++)
                {
                    jaggedArray[i][j] = array[i, j];
                }
            }

            return JsonSerializer.Serialize(jaggedArray);
        }
    }
}