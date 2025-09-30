using Xunit;

namespace Descope.Test.UnitTests
{
    public class UserStatusTests
    {
        [Fact]
        public void UserStatus_ToStringValue_ReturnsCorrectValues()
        {
            // Arrange & Act & Assert
            Assert.Equal("enabled", UserStatus.Enabled.ToStringValue());
            Assert.Equal("disabled", UserStatus.Disabled.ToStringValue());
            Assert.Equal("invited", UserStatus.Invited.ToStringValue());
        }

        [Theory]
        [InlineData(UserStatus.Enabled, "enabled")]
        [InlineData(UserStatus.Disabled, "disabled")]
        [InlineData(UserStatus.Invited, "invited")]
        public void UserStatus_ToStringValue_Theory(UserStatus status, string expected)
        {
            // Act
            var result = status.ToStringValue();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void UserStatus_ToStringValue_AllEnumValues_ReturnValidStrings()
        {
            // Arrange: Get all defined enum values
            var allEnumValues = Enum.GetValues<UserStatus>();
            var expectedResults = new Dictionary<UserStatus, string>
            {
                { UserStatus.Enabled, "enabled" },
                { UserStatus.Disabled, "disabled" },
                { UserStatus.Invited, "invited" }
            };

            // Act & Assert: Verify each enum value returns the expected string
            foreach (var enumValue in allEnumValues)
            {
                var result = enumValue.ToStringValue();
                Assert.True(expectedResults.ContainsKey(enumValue),
                    $"Missing expected result for enum value: {enumValue}. Please update the test with the expected string value.");
                Assert.Equal(expectedResults[enumValue], result);
            }

            // Ensure we're testing all values (no missing enum values in our expected results)
            Assert.Equal(expectedResults.Count, allEnumValues.Length);
        }
    }
}
