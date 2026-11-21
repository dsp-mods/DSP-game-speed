using System;

namespace DSP_Game_Speed
{
    /// <summary>
    /// Decides when the mod has to re-assert <c>Time.timeScale</c> (issue #9).
    /// </summary>
    /// <remarks>
    /// <c>Time.timeScale</c> is a single global that any code can write, and the last
    /// writer wins. Since DSP 0.6.17.5827 the game itself writes it every frame (its
    /// pause handling resets the scale to 1x), so the one-shot
    /// <c>Time.timeScale = curSpeed</c> in <c>SetGameSpeed</c> only lasts until the next
    /// frame: the speed briefly kicks in and then drops back to 1x. The fix is to
    /// re-assert the selected speed once per frame from the <c>GameMain.Update</c>
    /// postfix, which runs after the game's own update code.
    /// <para>
    /// This class has no UnityEngine dependency so it can be unit tested outside the
    /// game (see tests/). It must stay C# 7.3 / .NET Framework compatible, because the
    /// mod targets the game's Mono runtime.
    /// </para>
    /// </remarks>
    public static class SpeedReapplyPolicy
    {
        /// <summary>The game's default speed. At or below it the mod stays out of the way.</summary>
        public const float NormalSpeed = 1f;

        /// <summary>A time scale of 0 means the game is paused.</summary>
        public const float PausedTimeScale = 0f;

        /// <summary>
        /// Returns true when <paramref name="currentTimeScale"/> differs from the selected
        /// speed and the mod should write <paramref name="selectedSpeed"/> back into
        /// <c>Time.timeScale</c> this frame. Two cases are deliberately left alone:
        /// <list type="bullet">
        /// <item><paramref name="selectedSpeed"/> at or below 1x (including 0, negative and
        /// NaN): at 1x the mod must not fight the game's own time-scale writes.</item>
        /// <item><paramref name="currentTimeScale"/> of 0 (paused): Unity stops calling
        /// FixedUpdate at 0, so overriding it would unpause the game behind the player's
        /// back.</item>
        /// </list>
        /// </summary>
        /// <param name="selectedSpeed">The speed selected in the mod UI (UIPatch.curSpeed).</param>
        /// <param name="currentTimeScale">The value currently in <c>Time.timeScale</c>.</param>
        public static bool ShouldEnforce(float selectedSpeed, float currentTimeScale)
        {
            // NaN fails the comparison, so "not greater" also covers NaN input.
            if (!(selectedSpeed > NormalSpeed))
                return false;

            // Paused (or a corrupt negative value): never unpause behind the player's back.
            // NaN fails this comparison too and falls through to be corrected below.
            if (currentTimeScale <= PausedTimeScale)
                return false;

            // Already at the selected speed (within float tolerance): no write needed.
            if (Approximately(currentTimeScale, selectedSpeed))
                return false;

            return true;
        }

        /// <summary>
        /// Mirrors <c>UnityEngine.Mathf.Approximately</c> so the policy behaves identically
        /// inside the game and in the unit tests.
        /// </summary>
        public static bool Approximately(float a, float b)
        {
            return Math.Abs(b - a) < Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), float.Epsilon * 8f);
        }
    }
}
