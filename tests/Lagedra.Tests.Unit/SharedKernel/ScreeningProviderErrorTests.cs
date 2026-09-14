using FluentAssertions;
using Lagedra.SharedKernel.Integration.Screening;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class ScreeningProviderErrorTests
{
    [Fact]
    public void Phone_error_tells_host_what_checkr_rejected()
    {
        var message = ScreeningProviderError.ToHostMessage(
            422,
            [("/applicant.phone_number", "Applicant phone number is invalid")]);

        message.Should().Contain("Applicant phone number is invalid");
        message.Should().Contain("US mobile");
    }

    [Fact]
    public void State_error_tells_host_to_use_a_two_letter_code()
    {
        var message = ScreeningProviderError.ToHostMessage(
            422,
            [("/property.state", "State must be a valid US state code")]);

        message.Should().Contain("State must be a valid US state code");
        message.Should().Contain("CA");
    }

    [Fact]
    public void Unknown_checkr_detail_is_shown_as_is()
    {
        var message = ScreeningProviderError.ToHostMessage(
            422,
            [("/order.package", "must be one of: starter, essential")]);

        message.Should().Be("must be one of: starter, essential");
    }
}
