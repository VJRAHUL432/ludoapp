using UnityEngine;

namespace Ludo.Local
{
    /// <summary>
    /// Hardcoded 15x15 Ludo board geometry.
    /// Origin (0,0) is the TOP-LEFT cell. (col, row) form.
    /// </summary>
    public static class BoardCells
    {
        public const int GridSize = 15;
        public const int TrackLength = 52;
        public const int HomePathLength = 5;

        public static readonly Color[] SeatColors =
        {
            new Color(0.92f, 0.20f, 0.22f), // 0 = Red
            new Color(0.20f, 0.72f, 0.30f), // 1 = Green
            new Color(0.96f, 0.78f, 0.16f), // 2 = Yellow
            new Color(0.18f, 0.45f, 0.92f), // 3 = Blue
        };

        // Each color exits onto cell (StartCell[seat]) of the main 52-cell loop.
        public static readonly int[] StartCell = { 0, 13, 26, 39 };

        // Last main-track cell before entering the seat's home column.
        public static readonly int[] HomeEntry = { 51, 12, 25, 38 };

        // Safe cells (start cells + 4 stars). Tokens here cannot be killed.
        public static readonly System.Collections.Generic.HashSet<int> SafeCells =
            new System.Collections.Generic.HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

        /// <summary>52 main-track cell coordinates, clockwise from Red's start.</summary>
        public static readonly Vector2Int[] Track = new Vector2Int[]
        {
            new Vector2Int(1,6),  new Vector2Int(2,6),  new Vector2Int(3,6),  new Vector2Int(4,6),  new Vector2Int(5,6),  // 0..4   left arm top edge
            new Vector2Int(6,5),  new Vector2Int(6,4),  new Vector2Int(6,3),  new Vector2Int(6,2),  new Vector2Int(6,1),  new Vector2Int(6,0),  // 5..10  green arm left edge
            new Vector2Int(7,0),  new Vector2Int(8,0),  // 11..12 top center crossing
            new Vector2Int(8,1),  new Vector2Int(8,2),  new Vector2Int(8,3),  new Vector2Int(8,4),  new Vector2Int(8,5),  // 13..17 green arm right edge (Green start at 13)
            new Vector2Int(9,6),  new Vector2Int(10,6), new Vector2Int(11,6), new Vector2Int(12,6), new Vector2Int(13,6), new Vector2Int(14,6), // 18..23 right arm top
            new Vector2Int(14,7), new Vector2Int(14,8), // 24..25
            new Vector2Int(13,8), new Vector2Int(12,8), new Vector2Int(11,8), new Vector2Int(10,8), new Vector2Int(9,8),  // 26..30 right arm bottom (Yellow start at 26)
            new Vector2Int(8,9),  new Vector2Int(8,10), new Vector2Int(8,11), new Vector2Int(8,12), new Vector2Int(8,13), new Vector2Int(8,14), // 31..36 yellow arm right edge
            new Vector2Int(7,14), new Vector2Int(6,14), // 37..38
            new Vector2Int(6,13), new Vector2Int(6,12), new Vector2Int(6,11), new Vector2Int(6,10), new Vector2Int(6,9),  // 39..43 yellow arm left edge (Blue start at 39)
            new Vector2Int(5,8),  new Vector2Int(4,8),  new Vector2Int(3,8),  new Vector2Int(2,8),  new Vector2Int(1,8),  new Vector2Int(0,8),  // 44..49 left arm bottom
            new Vector2Int(0,7),  new Vector2Int(0,6),  // 50..51
        };

        /// <summary>Home columns (5 cells each), index 0 = first cell after entering, index 4 = adjacent to center.</summary>
        public static readonly Vector2Int[][] HomeColumn = new Vector2Int[][]
        {
            // Red — enters from cell 51 = (0,6), then walks rightward into the middle row 7
            new[] { new Vector2Int(1,7), new Vector2Int(2,7), new Vector2Int(3,7), new Vector2Int(4,7), new Vector2Int(5,7) },
            // Green — enters from cell 12 = (8,0), then walks downward in column 7
            new[] { new Vector2Int(7,1), new Vector2Int(7,2), new Vector2Int(7,3), new Vector2Int(7,4), new Vector2Int(7,5) },
            // Yellow — enters from cell 25 = (14,8), then walks leftward in row 7
            new[] { new Vector2Int(13,7), new Vector2Int(12,7), new Vector2Int(11,7), new Vector2Int(10,7), new Vector2Int(9,7) },
            // Blue — enters from cell 38 = (6,14), then walks upward in column 7
            new[] { new Vector2Int(7,13), new Vector2Int(7,12), new Vector2Int(7,11), new Vector2Int(7,10), new Vector2Int(7,9) },
        };

        /// <summary>Center "finish" cell where finished tokens stack.</summary>
        public static readonly Vector2 FinishCenter = new Vector2(7f, 7f);

        /// <summary>4 base slots per seat. These are positions inside the seat's 6x6 corner.</summary>
        public static readonly Vector2[][] BaseSlots = new Vector2[][]
        {
            // Red — top-left corner (cols 0-5, rows 0-5)
            new[] { new Vector2(1.5f, 1.5f), new Vector2(3.5f, 1.5f), new Vector2(1.5f, 3.5f), new Vector2(3.5f, 3.5f) },
            // Green — top-right (cols 9-14, rows 0-5)
            new[] { new Vector2(10.5f, 1.5f), new Vector2(12.5f, 1.5f), new Vector2(10.5f, 3.5f), new Vector2(12.5f, 3.5f) },
            // Yellow — bottom-right (cols 9-14, rows 9-14)
            new[] { new Vector2(10.5f, 10.5f), new Vector2(12.5f, 10.5f), new Vector2(10.5f, 12.5f), new Vector2(12.5f, 12.5f) },
            // Blue — bottom-left (cols 0-5, rows 9-14)
            new[] { new Vector2(1.5f, 10.5f), new Vector2(3.5f, 10.5f), new Vector2(1.5f, 12.5f), new Vector2(3.5f, 12.5f) },
        };

        public static bool IsSafeCell(int trackIndex) => SafeCells.Contains(trackIndex);

        /// <summary>
        /// Distance walked along the seat's loop from its start cell to a given track index.
        /// Range 0..51.
        /// </summary>
        public static int DistanceFromStart(int seat, int trackIndex)
        {
            return ((trackIndex - StartCell[seat]) % TrackLength + TrackLength) % TrackLength;
        }
    }
}
