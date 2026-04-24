using KnowHub.Application.Models.Surveys;
using KnowHub.Application.Validators.Surveys;
using KnowHub.Domain.Enums;

namespace KnowHub.Tests.Validators;

public class SurveyValidatorTests
{
    // -- CreateSurveyRequestValidator ---------------------------------------

    [Fact]
    public async Task CreateSurveyRequest_ValidRequest_Passes()
    {
        var validator = new CreateSurveyRequestValidator();
        var result = await validator.ValidateAsync(
            new CreateSurveyRequest("My Survey", null, null, null, null, false));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateSurveyRequest_EmptyTitle_Fails()
    {
        var validator = new CreateSurveyRequestValidator();
        var result = await validator.ValidateAsync(
            new CreateSurveyRequest("", null, null, null, null, false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task CreateSurveyRequest_EndsAt_InPast_Fails()
    {
        var validator = new CreateSurveyRequestValidator();
        var result = await validator.ValidateAsync(
            new CreateSurveyRequest("Survey", null, null, null, DateTime.UtcNow.AddDays(-1), false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "EndsAt");
    }

    [Fact]
    public async Task CreateSurveyRequest_EndsAt_InFuture_Passes()
    {
        var validator = new CreateSurveyRequestValidator();
        var result = await validator.ValidateAsync(
            new CreateSurveyRequest("Survey", null, null, null, DateTime.UtcNow.AddDays(14), false));
        Assert.True(result.IsValid);
    }

    // -- AddSurveyQuestionRequestValidator ----------------------------------

    [Fact]
    public async Task AddSurveyQuestionRequest_ValidRating_Passes()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("Rate this", SurveyQuestionType.Rating, null, 1, 5, true, 0));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AddSurveyQuestionRequest_MultiChoiceWithNoOptions_Fails()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("Pick one", SurveyQuestionType.MultipleChoice,
                null, 1, 5, true, 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AddSurveyQuestionRequest_SingleChoiceWithNoOptions_Fails()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("Select", SurveyQuestionType.SingleChoice,
                null, 1, 5, true, 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AddSurveyQuestionRequest_ChoiceWithTwoOptions_Passes()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("Pick one", SurveyQuestionType.SingleChoice,
                new List<string> { "Option A", "Option B" }, 1, 5, true, 0));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AddSurveyQuestionRequest_RatingWithInvertedRange_Fails()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        // MinRating > MaxRating
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("Rate", SurveyQuestionType.Rating,
                null, 5, 3, true, 0));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxRating");
    }

    [Fact]
    public async Task AddSurveyQuestionRequest_RatingWithEqualMinMax_Fails()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        // MinRating == MaxRating (must be strictly less)
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("Rate", SurveyQuestionType.Rating,
                null, 5, 5, true, 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AddSurveyQuestionRequest_NpsRating_Passes()
    {
        var validator = new AddSurveyQuestionRequestValidator();
        // NPS: 0–10
        var result = await validator.ValidateAsync(
            new AddSurveyQuestionRequest("How likely to recommend?", SurveyQuestionType.Rating,
                null, 0, 10, true, 0));
        Assert.True(result.IsValid);
    }

    // -- SubmitSurveyRequestValidator ---------------------------------------

    [Fact]
    public async Task SubmitSurveyRequest_EmptyAnswers_Fails()
    {
        var validator = new SubmitSurveyRequestValidator();
        var result = await validator.ValidateAsync(
            new SubmitSurveyRequest(new List<SurveyAnswerRequest>()));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Answers");
    }

    [Fact]
    public async Task SubmitSurveyRequest_WithAnswers_Passes()
    {
        var validator = new SubmitSurveyRequestValidator();
        var result = await validator.ValidateAsync(
            new SubmitSurveyRequest(new List<SurveyAnswerRequest>
            {
                new(Guid.NewGuid(), null, null, 5)
            }));
        Assert.True(result.IsValid);
    }
}
