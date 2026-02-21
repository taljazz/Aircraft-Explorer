namespace AircraftExplorer.Education;

public class QuizSession
{
    public int QuestionsAnswered { get; private set; }
    public int CorrectAnswers { get; private set; }

    public void RecordAnswer(bool correct)
    {
        QuestionsAnswered++;
        if (correct) CorrectAnswers++;
    }

    public string GetScoreSummary()
    {
        if (QuestionsAnswered == 0)
            return "No questions answered yet.";

        int percentage = (int)Math.Round(100.0 * CorrectAnswers / QuestionsAnswered);
        return $"Overall score: {CorrectAnswers} of {QuestionsAnswered} correct, {percentage} percent.";
    }
}
