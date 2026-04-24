using KnowHub.Application.Contracts;
using KnowHub.Application.Validators;
using KnowHub.Domain.Enums;

namespace KnowHub.Tests.Validators;

public class ValidatorTests
{
    [Fact]
    public async Task LoginRequestValidator_ValidRequest_Passes()
    {
        var validator = new LoginRequestValidator();
        var result = await validator.ValidateAsync(new LoginRequest { Email = "user@test.com", Password = "password" });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("user@test.com", "")]
    public async Task LoginRequestValidator_InvalidRequest_Fails(string email, string password)
    {
        var validator = new LoginRequestValidator();
        var result = await validator.ValidateAsync(new LoginRequest { Email = email, Password = password });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateTagValidator_ValidTagName_Passes()
    {
        var validator = new CreateTagRequestValidator();
        var result = await validator.ValidateAsync(new CreateTagRequest { Name = "React123" });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("React & Angular")]
    [InlineData("Tag-with-dashes")]
    public async Task CreateTagValidator_InvalidTagName_Fails(string name)
    {
        var validator = new CreateTagRequestValidator();
        var result = await validator.ValidateAsync(new CreateTagRequest { Name = name });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateSessionProposalValidator_ValidRequest_Passes()
    {
        var validator = new CreateSessionProposalRequestValidator();
        var result = await validator.ValidateAsync(new CreateSessionProposalRequest
        {
            Title = "My Session",
            CategoryId = Guid.NewGuid(),
            Topic = "Architecture",
            Format = SessionFormat.Webinar,
            EstimatedDurationMinutes = 60,
            DifficultyLevel = DifficultyLevel.Intermediate
        });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(481)]
    public async Task CreateSessionProposalValidator_InvalidDuration_Fails(int duration)
    {
        var validator = new CreateSessionProposalRequestValidator();
        var result = await validator.ValidateAsync(new CreateSessionProposalRequest
        {
            Title = "My Session",
            CategoryId = Guid.NewGuid(),
            Topic = "Architecture",
            Format = SessionFormat.Webinar,
            EstimatedDurationMinutes = duration,
            DifficultyLevel = DifficultyLevel.Beginner
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task SubmitRatingValidator_ScoresOutOfRange_Fails()
    {
        var validator = new SubmitSessionRatingRequestValidator();

        var resultLow = await validator.ValidateAsync(new SubmitSessionRatingRequest { SessionScore = 0, SpeakerScore = 3 });
        Assert.False(resultLow.IsValid);

        var resultHigh = await validator.ValidateAsync(new SubmitSessionRatingRequest { SessionScore = 3, SpeakerScore = 6 });
        Assert.False(resultHigh.IsValid);
    }

    [Fact]
    public async Task SubmitRatingValidator_ValidScores_Passes()
    {
        var validator = new SubmitSessionRatingRequestValidator();
        var result = await validator.ValidateAsync(new SubmitSessionRatingRequest { SessionScore = 5, SpeakerScore = 4 });
        Assert.True(result.IsValid);
    }
}
