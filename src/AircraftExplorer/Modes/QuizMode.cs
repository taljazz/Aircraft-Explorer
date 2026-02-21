using AircraftExplorer.Education;
using AircraftExplorer.Input;

namespace AircraftExplorer.Modes;

public class QuizMode : IAppMode
{
    private readonly IReadOnlyList<QuizQuestion> _questions;
    private int _questionIndex;
    private int _selectedOption;
    private bool _answered;
    private int _correctCount;
    private ModeContext _context = null!;

    public string ModeName => "Quiz";

    public QuizMode(IReadOnlyList<QuizQuestion> questions)
    {
        _questions = questions;
    }

    public void OnEnter(ModeContext context)
    {
        _context = context;
        _questionIndex = 0;
        _correctCount = 0;
        _answered = false;
        _selectedOption = 0;

        context.Speech.Speak(
            $"Quiz! {_questions.Count} questions. Use Up and Down to select an answer, Enter to submit. Escape to quit.",
            true);

        AnnounceCurrentQuestion();
    }

    public ModeResult HandleInput(InputAction action)
    {
        if (_answered)
        {
            // After answering, Enter advances to next question
            if (action == InputAction.Select)
            {
                _questionIndex++;
                if (_questionIndex >= _questions.Count)
                {
                    AnnounceFinished();
                    return ModeResult.Pop;
                }
                _answered = false;
                _selectedOption = 0;
                AnnounceCurrentQuestion();
                return ModeResult.Stay;
            }

            if (action == InputAction.Back)
                return ModeResult.Pop;

            return ModeResult.Stay;
        }

        var question = _questions[_questionIndex];

        switch (action)
        {
            case InputAction.MoveForward:
            case InputAction.MenuUp:
            case InputAction.MoveUp:
                _selectedOption = (_selectedOption - 1 + question.Options.Count) % question.Options.Count;
                _context.Speech.Speak(
                    $"Option {_selectedOption + 1}: {question.Options[_selectedOption]}",
                    true);
                return ModeResult.Stay;

            case InputAction.MoveBackward:
            case InputAction.MenuDown:
            case InputAction.MoveDown:
                _selectedOption = (_selectedOption + 1) % question.Options.Count;
                _context.Speech.Speak(
                    $"Option {_selectedOption + 1}: {question.Options[_selectedOption]}",
                    true);
                return ModeResult.Stay;

            case InputAction.Select:
                SubmitAnswer(question);
                return ModeResult.Stay;

            case InputAction.Back:
                return ModeResult.Pop;

            case InputAction.Help:
                _context.Speech.Speak(
                    "Quiz mode. Up and Down to browse options. Enter to submit your answer. Escape to quit.",
                    true);
                return ModeResult.Stay;

            default:
                return ModeResult.Stay;
        }
    }

    private void AnnounceCurrentQuestion()
    {
        var question = _questions[_questionIndex];
        var text = $"Question {_questionIndex + 1} of {_questions.Count}. {question.Question} " +
                   $"Option 1: {question.Options[0]}";
        _context.Speech.Speak(text, true);
    }

    private void SubmitAnswer(QuizQuestion question)
    {
        bool correct = _selectedOption == question.CorrectIndex;
        _answered = true;

        if (correct)
        {
            _correctCount++;
            _context.SpatialAudio.PlayCorrectAnswerTone();
            _context.Speech.Speak(
                $"Correct! {question.Explanation} " +
                $"Score: {_correctCount} of {_questionIndex + 1}. Press Enter to continue.",
                false);
        }
        else
        {
            _context.SpatialAudio.PlayIncorrectAnswerTone();
            string correctAnswer = question.Options[question.CorrectIndex];
            _context.Speech.Speak(
                $"Incorrect. The answer is {correctAnswer}. {question.Explanation} " +
                $"Score: {_correctCount} of {_questionIndex + 1}. Press Enter to continue.",
                false);
        }

        _context.QuizSession.RecordAnswer(correct);
    }

    private void AnnounceFinished()
    {
        int percentage = _questions.Count > 0
            ? (int)Math.Round(100.0 * _correctCount / _questions.Count)
            : 0;
        _context.Speech.Speak(
            $"Quiz complete! You scored {_correctCount} out of {_questions.Count}, {percentage} percent. " +
            _context.QuizSession.GetScoreSummary(),
            true);
    }

    public void OnExit() { }
    public void OnResume() { }
}
