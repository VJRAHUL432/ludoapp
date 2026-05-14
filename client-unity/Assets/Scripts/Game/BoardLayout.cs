using UnityEngine;

namespace Ludo.Game
{
    /// <summary>
    /// Mirrors server-side board.js. Use to translate
    /// (seat, token state) → world position for rendering.
    /// </summary>
    public static class BoardLayout
    {
        public const int SeatCount = 4;
        public const int TokensPerSeat = 4;
        public const int TrackLength = 52;
        public const int HomePathLength = 5;

        public static readonly int[] StartCell = { 0, 13, 26, 39 };
        public static readonly int[] HomeEntry = { 51, 12, 25, 38 };

        private static readonly System.Collections.Generic.HashSet<int> _safeCells =
            new System.Collections.Generic.HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

        public static bool IsSafeCell(int trackIndex) => _safeCells.Contains(trackIndex);

        /// <summary>
        /// World positions for the 52 main-track cells.
        /// Wire these from a serialized BoardConfig ScriptableObject so artists
        /// can tweak without touching code. This default is just a placeholder.
        /// </summary>
        public static Vector2 GetMainTrackPosition(int trackIndex, BoardConfig cfg)
        {
            if (cfg == null || cfg.mainTrack == null || cfg.mainTrack.Length == 0)
                return Vector2.zero;
            return cfg.mainTrack[Mathf.Clamp(trackIndex, 0, cfg.mainTrack.Length - 1)];
        }

        public static Vector2 GetHomePathPosition(int seat, int homeIndex, BoardConfig cfg)
        {
            if (cfg == null || cfg.homePaths == null || seat < 0 || seat >= cfg.homePaths.Length)
                return Vector2.zero;
            var path = cfg.homePaths[seat].cells;
            if (path == null || path.Length == 0) return Vector2.zero;
            return path[Mathf.Clamp(homeIndex, 0, path.Length - 1)];
        }

        public static Vector2 GetBasePosition(int seat, int slot, BoardConfig cfg)
        {
            if (cfg == null || cfg.bases == null || seat < 0 || seat >= cfg.bases.Length)
                return Vector2.zero;
            var basePts = cfg.bases[seat].slots;
            if (basePts == null || basePts.Length == 0) return Vector2.zero;
            return basePts[Mathf.Clamp(slot, 0, basePts.Length - 1)];
        }

        public static Vector2 GetFinishPosition(int seat, BoardConfig cfg)
        {
            if (cfg == null || cfg.finishCenters == null || seat < 0 || seat >= cfg.finishCenters.Length)
                return Vector2.zero;
            return cfg.finishCenters[seat];
        }
    }

    [CreateAssetMenu(menuName = "Ludo/Board Config")]
    public class BoardConfig : ScriptableObject
    {
        public Vector2[] mainTrack;       // length 52
        public HomePath[] homePaths;      // length 4
        public Base[] bases;              // length 4
        public Vector2[] finishCenters;   // length 4

        [System.Serializable] public class HomePath { public Vector2[] cells; }
        [System.Serializable] public class Base { public Vector2[] slots; }
    }
}
