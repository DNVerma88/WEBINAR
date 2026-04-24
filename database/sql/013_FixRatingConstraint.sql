-- Migration 013: Fix CHK_SurveyQuestions_RatingRange constraint
-- The original constraint enforced MinRating < MaxRating for ALL question types,
-- causing a 23514 violation when inserting Text/Choice/YesNo questions with
-- MinRating=0, MaxRating=0. The constraint must only apply to Rating questions.

ALTER TABLE "SurveyQuestions"
    DROP CONSTRAINT IF EXISTS "CHK_SurveyQuestions_RatingRange";

ALTER TABLE "SurveyQuestions"
    ADD CONSTRAINT "CHK_SurveyQuestions_RatingRange"
        CHECK (
            "QuestionType" <> 'Rating'
            OR ("MinRating" >= 0 AND "MaxRating" <= 10 AND "MinRating" < "MaxRating")
        );
