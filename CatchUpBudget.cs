using System;

namespace DSP_Game_Speed
{
    /// <summary>
    /// Computes the Time.maximumDeltaTime the game needs so a speed above 1x is
    /// actually delivered instead of being silently capped (issues #13, #18).
    /// </summary>
    /// <remarks>
    /// Unity timing background:
    /// <list type="bullet">
    /// <item>Time.timeScale = N does not shrink the FixedUpdate step. It asks for about N
    /// times as many fixed steps of Time.fixedDeltaTime per rendered frame.</item>
    /// <item>Time.maximumDeltaTime caps the scaled time that may elapse in one frame,
    /// and so caps FixedUpdate calls per frame at maximumDeltaTime / fixedDeltaTime.
    /// Scaled time per frame is N * realFrameTime, so once that exceeds the cap the
    /// surplus is dropped. Unity's default cap of 1/3 s sustains at most 10x at
    /// 30 FPS, or 20x at 60 FPS.</item>
    /// </list>
    /// This class has no UnityEngine dependency so it can be unit tested outside the
    /// game (see tests/). It must stay C# 7.3 / .NET Framework compatible, because
    /// the mod targets the game's Mono runtime.
    /// </remarks>
    public static class CatchUpBudget
    {
        /// <summary>The game's default speed. At or below it, no extra budget is needed.</summary>
        public const float NormalSpeed = 1f;

        /// <summary>
        /// Lowest frame rate at which the selected speed should still be fully delivered.
        /// Below this, Unity's cap applies again and the game runs slower than
        /// selected instead of stalling.
        /// </summary>
        public const float MinFullSpeedFrameRate = 30f;

        /// <summary>
        /// Ceiling, in seconds of game time per frame, on the budget we request. Without
        /// it, one slow frame schedules more fixed steps, which makes the next frame
        /// slower still (the "spiral of death").
        /// </summary>
        public const float MaxSecondsPerFrame = 1f;

        /// <summary>
        /// Returns the Time.maximumDeltaTime to use for <paramref name="speed"/>. The
        /// result is never below the game's own <paramref name="baselineMaximumDeltaTime"/>
        /// and never above <see cref="MaxSecondsPerFrame"/> (unless the baseline itself
        /// is higher).
        /// </summary>
        /// <param name="speed">The selected Time.timeScale. 0 (pause), 1x, NaN, and
        /// negative values all return the baseline unchanged.</param>
        /// <param name="baselineMaximumDeltaTime">The game's value before the mod changed it.</param>
        public static float RequiredMaximumDeltaTime(float speed, float baselineMaximumDeltaTime)
        {
            // NaN fails every comparison, so test "not greater" rather than "less or equal".
            if (!(speed > NormalSpeed))
            {
                return baselineMaximumDeltaTime;
            }

            // Math.Min also bounds +Infinity.
            float needed = Math.Min(speed / MinFullSpeedFrameRate, MaxSecondsPerFrame);
            return Math.Max(baselineMaximumDeltaTime, needed);
        }
    }
}
