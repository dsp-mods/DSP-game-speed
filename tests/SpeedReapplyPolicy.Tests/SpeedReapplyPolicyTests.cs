using Xunit;

namespace DSP_Game_Speed.Tests
{
    public class SpeedReapplyPolicyTests
    {
        private const float Precision = 1e-6f;

        // --- ShouldEnforce: the 0.6.17.5827 regression (issue #9) ---
        // The game writes Time.timeScale = 1 every frame behind the mod's back, so a
        // one-shot write in SetGameSpeed only lasts one frame. The per-frame postfix
        // must re-assert the selected speed whenever the game reset it.

        [Theory]
        [InlineData(2f)]
        [InlineData(3f)]
        [InlineData(4f)]   // default customSpeed
        [InlineData(25f)]
        public void GameResetTimeScaleTo1x_ModReassertsSelectedSpeed(float selectedSpeed)
        {
            Assert.True(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, 1f));
        }

        [Theory]
        [InlineData(2f, 0.5f)]
        [InlineData(3f, 2.5f)]
        [InlineData(4f, float.NaN)] // a corrupt value is corrected too
        public void TimeScaleDriftedFromSelectedSpeed_ModReasserts(float selectedSpeed, float current)
        {
            Assert.True(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, current));
        }

        // --- ShouldEnforce: cases where the mod must stay out of the way ---

        [Theory]
        [InlineData(1f, 1f)]
        [InlineData(1f, 0f)]
        [InlineData(1f, 3f)]   // another mod raised it; at 1x the mod does not clamp it down
        [InlineData(0f, 1f)]
        [InlineData(-2f, 1f)]
        [InlineData(float.NaN, 1f)]
        public void SelectedSpeedAtOrBelow1x_NeverEnforced(float selectedSpeed, float current)
        {
            Assert.False(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, current));
        }

        [Theory]
        [InlineData(2f, 0f)]
        [InlineData(3f, 0f)]
        [InlineData(25f, -1f)]
        public void GamePaused_NeverUnpausedBehindPlayersBack(float selectedSpeed, float current)
        {
            // timeScale 0 = paused; Unity stops FixedUpdate at 0, so overriding it
            // would unpause the game behind the player's back.
            Assert.False(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, current));
        }

        [Theory]
        [InlineData(2f, 2f)]
        [InlineData(3f, 3f)]
        [InlineData(4f, 4f + 1e-7f)] // within Mathf.Approximately tolerance
        public void AlreadyAtSelectedSpeed_NoRedundantWrite(float selectedSpeed, float current)
        {
            Assert.False(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, current));
        }

        // --- Frame-by-frame simulation of the issue #9 scenario ---

        [Fact]
        public void GameResettingTimeScaleEveryFrame_SpeedStaysApplied()
        {
            float selectedSpeed = 3f;
            float timeScale = 1f;

            for (int frame = 0; frame < 120; frame++)
            {
                // Game code (0.6.17.5827+) resets the global each frame...
                timeScale = 1f;
                // ...then the GameMain.Update postfix runs and re-asserts the selection.
                if (SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, timeScale))
                    timeScale = selectedSpeed;

                Assert.Equal(selectedSpeed, timeScale, Precision);
            }
        }

        [Fact]
        public void PauseDuringFastSpeed_StaysPausedUntilGameResumes()
        {
            float selectedSpeed = 3f;
            float timeScale = 3f;

            // Player pauses: the game writes 0. The mod must not override it.
            timeScale = 0f;
            Assert.False(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, timeScale));

            // Player resumes (game writes 1): the mod re-asserts 3x on the next frame.
            timeScale = 1f;
            Assert.True(SpeedReapplyPolicy.ShouldEnforce(selectedSpeed, timeScale));
        }

        // --- Approximately mirrors UnityEngine.Mathf.Approximately ---

        [Theory]
        [InlineData(1f, 1f)]
        [InlineData(3f, 3f + 1e-7f)]
        [InlineData(-2f, -2f - 1e-7f)]
        public void Approximately_EqualWithinTolerance(float a, float b)
        {
            Assert.True(SpeedReapplyPolicy.Approximately(a, b));
        }

        [Theory]
        [InlineData(1f, 1f + 1e-4f)]
        [InlineData(3f, 2.9f)]
        [InlineData(float.NaN, 1f)]
        [InlineData(1f, float.NaN)]
        public void Approximately_DifferentBeyondTolerance(float a, float b)
        {
            Assert.False(SpeedReapplyPolicy.Approximately(a, b));
        }
    }
}
