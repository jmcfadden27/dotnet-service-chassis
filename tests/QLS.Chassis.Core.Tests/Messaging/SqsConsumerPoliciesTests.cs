using QLS.Chassis.Core.Messaging.Consumption;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class SqsConsumerPoliciesTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var policy = SqsConsumerPolicies.Default;

        Assert.Equal(10, policy.MaxNumberOfMessages);
        Assert.Equal(20, policy.WaitTimeSeconds);
        Assert.Equal(120, policy.VisibilityTimeoutSeconds);
        Assert.Equal(3, policy.ErrorDelaySeconds);
        Assert.Single(policy.MessageAttributeNames);
        Assert.Equal("All", policy.MessageAttributeNames[0]);
    }

    [Fact]
    public void SqsConsumerPolicy_CanBeConfiguredViaInitProperties()
    {
        ISqsConsumerPolicy policy = new SqsConsumerPolicy
        {
            MaxNumberOfMessages = 5,
            WaitTimeSeconds = 10,
            VisibilityTimeoutSeconds = 30,
            ErrorDelaySeconds = 1,
            MessageAttributeNames = ["All"],
        };

        Assert.Equal(5, policy.MaxNumberOfMessages);
        Assert.Equal(10, policy.WaitTimeSeconds);
        Assert.Equal(30, policy.VisibilityTimeoutSeconds);
        Assert.Equal(1, policy.ErrorDelaySeconds);
        Assert.Single(policy.MessageAttributeNames);
    }
}
