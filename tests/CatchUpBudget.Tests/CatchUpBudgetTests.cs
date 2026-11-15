using Xunit;

namespace DSP_Game_Speed.Tests
{
    public class CatchUpBudgetTests
    {
        // Unity's default Time.maximumDeltaTime.
        private const float UnityDefaultMaximumDeltaTime = 1f / 3f;
        private const float Precision = 1e-6f;

        [Theory]
        [InlineData(0f)]    // Pause: FixedUpdate does not run, so no budget needed.
        [InlineData(0.5f)]  // Slow motion.
        [InlineData(1f)]    // Normal speed.
        [InlineData(-3f)]   // Invalid input.
        [InlineData(float.NaN)]
        public void AtOrBelowNormalSpeed_ReturnsBaseline(float speed)
        {
            float result = CatchUpBudget.RequiredMaximumDeltaTime(speed, UnityDefaultMaximumDeltaTime);

            Assert.Equal(UnityDefaultMaximumDeltaTime, result, Precision);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(3f)]
        [InlineData(4f)]  // Default customSpeed.
        [InlineData(10f)] // Exactly sustainable at 30 FPS by the default cap.
        public void LowPresetSpeeds_KeepGameBaseline(float speed)
        {
            // speed / 30 FPS <= 1/3 s, so the default cap already suffices.
            float result = CatchUpBudget.RequiredMaximumDeltaTime(speed, UnityDefaultMaximumDeltaTime);

            Assert.Equal(UnityDefaultMaximumDeltaTime, result, Precision);
        }

        [Theory]
        [InlineData(15f, 0.5f)]
        [InlineData(20f, 20f / 30f)]
        [InlineData(25f, 25f / 30f)] // Speed used in issue #16.
        public void CustomSpeedsAboveDefaultCap_RaiseBudgetToSpeedOverMinFrameRate(
            float speed, float expected)
        {
            float result = CatchUpBudget.RequiredMaximumDeltaTime(speed, UnityDefaultMaximumDeltaTime);

            Assert.Equal(expected, result, Precision);
        }

        [Theory]
        [InlineData(30f)]
        [InlineData(100f)]
        [InlineData(5000f)] // Old config clamp.
        [InlineData(float.PositiveInfinity)]
        public void VeryHighSpeeds_AreBoundedByCeiling(float speed)
        {
            float result = CatchUpBudget.RequiredMaximumDeltaTime(speed, UnityDefaultMaximumDeltaTime);

            Assert.Equal(CatchUpBudget.MaxSecondsPerFrame, result, Precision);
        }

        [Fact]
        public void NeverLowersAGameBaselineAboveTheComputedBudget()
        {
            const float generousBaseline = 2f;

            float result = CatchUpBudget.RequiredMaximumDeltaTime(20f, generousBaseline);

            Assert.Equal(generousBaseline, result, Precision);
        }

        [Fact]
        public void BudgetSustainsSelectedSpeedAtMinFrameRate()
        {
            // Scaled time per frame at the minimum supported frame rate must fit in the budget.
            const float speed = 25f;
            float scaledSecondsPerFrame = speed / CatchUpBudget.MinFullSpeedFrameRate;

            float result = CatchUpBudget.RequiredMaximumDeltaTime(speed, UnityDefaultMaximumDeltaTime);

            Assert.True(result >= scaledSecondsPerFrame - Precision);
        }
    }
}
