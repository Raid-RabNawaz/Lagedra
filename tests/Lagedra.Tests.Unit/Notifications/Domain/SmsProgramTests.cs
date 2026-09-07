using FluentAssertions;
using Lagedra.Modules.Notifications.Domain;
using Xunit;

namespace Lagedra.Tests.Unit.Notifications.Domain;

public class SmsProgramTests
{
    [Theory]
    [InlineData("STOP")]
    [InlineData("stop")]
    [InlineData("STOPALL")]
    [InlineData("UNSUBSCRIBE")]
    [InlineData("CANCEL")]
    [InlineData("END")]
    [InlineData("QUIT")]
    public void Recognizes_stop_keywords(string body)
    {
        SmsProgram.IsStopKeyword(body).Should().BeTrue();
        SmsProgram.IsStartKeyword(body).Should().BeFalse();
        SmsProgram.IsHelpKeyword(body).Should().BeFalse();
    }

    [Theory]
    [InlineData("START")]
    [InlineData("unstop")]
    [InlineData("YES")]
    public void Recognizes_start_keywords(string body)
    {
        SmsProgram.IsStartKeyword(body).Should().BeTrue();
        SmsProgram.IsStopKeyword(body).Should().BeFalse();
    }

    [Theory]
    [InlineData("HELP")]
    [InlineData("info")]
    public void Recognizes_help_keywords(string body)
    {
        SmsProgram.IsHelpKeyword(body).Should().BeTrue();
        SmsProgram.IsStopKeyword(body).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello there")]
    [InlineData("STOP please")]
    public void Ignores_non_keyword_bodies(string? body)
    {
        SmsProgram.IsStopKeyword(body).Should().BeFalse();
        SmsProgram.IsStartKeyword(body).Should().BeFalse();
        SmsProgram.IsHelpKeyword(body).Should().BeFalse();
    }

    [Fact]
    public void Help_reply_includes_frequency_rates_and_contact()
    {
        SmsProgram.Frequency.Should().Be("up to 8 messages per month");
        SmsProgram.HelpReply.Should().Contain("STOP");
        SmsProgram.HelpReply.Should().Contain("HELP");
        SmsProgram.HelpReply.Should().Contain("info@lagedra.com");
        SmsProgram.HelpReply.Should().Contain("213-735-2362");
    }
}
