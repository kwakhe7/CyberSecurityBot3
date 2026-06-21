using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatbotPart2
{
    /// <summary>
    /// Manages quiz sessions (question bank, session state, scoring).
    /// Designed to be UI-agnostic so the WPF layer can call StartQuiz / GetCurrentQuestion / AnswerCurrentQuestion.
    /// </summary>
    public class QuizManager
    {
        private readonly Random _rng;
        private readonly List<QuizQuestion> _quizBank;

        // Session state
        private List<QuizQuestion> _currentQuiz = new();
        private int _quizIndex = -1;
        private int _quizScore = 0;
        private readonly object _lock = new();
        private bool _active = false;

        public QuizManager(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
            _quizBank = CreateDefaultBank();
        }

        // Public model for a question
        public class QuizQuestion
        {
            public string Question { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
            public int CorrectOptionIndex { get; set; }
            public string Explanation { get; set; } = string.Empty;

            public QuizQuestion(string question, IEnumerable<string> options, int correctIndex, string explanation = "")
            {
                Question = question ?? string.Empty;
                Options = options?.ToList() ?? new List<string>();
                CorrectOptionIndex = correctIndex;
                Explanation = explanation ?? string.Empty;
            }
        }

        // Start a quiz session. If numQuestions is null or >= bank size, uses full bank.
        public void StartQuiz(int? numQuestions = 10, bool shuffle = true)
        {
            lock (_lock)
            {
                var bank = _quizBank.ToList();

                if (shuffle)
                {
                    // Fisher-Yates
                    for (int i = bank.Count - 1; i > 0; i--)
                    {
                        int j = _rng.Next(i + 1);
                        var tmp = bank[i];
                        bank[i] = bank[j];
                        bank[j] = tmp;
                    }
                }

                _currentQuiz = (numQuestions.HasValue && numQuestions.Value > 0 && numQuestions.Value < bank.Count)
                    ? bank.Take(numQuestions.Value).ToList()
                    : bank;

                _quizIndex = 0;
                _quizScore = 0;
                _active = _currentQuiz.Count > 0;
            }
        }

        // Returns current question or null if no active quiz
        public QuizQuestion? GetCurrentQuestion()
        {
            lock (_lock)
            {
                if (!_active || _quizIndex < 0 || _quizIndex >= _currentQuiz.Count) return null;
                return _currentQuiz[_quizIndex];
            }
        }

        // Expose current index (zero-based) and total questions for UI counters
        public int CurrentQuestionIndex
        {
            get { lock (_lock) { return _quizIndex; } }
        }

        public int TotalQuestions
        {
            get { lock (_lock) { return _currentQuiz.Count; } }
        }

        // Submit an answer (zero-based index). Returns (isCorrect, feedback, finished).
        public (bool isCorrect, string feedback, bool finished) AnswerCurrentQuestion(int selectedOptionIndex)
        {
            lock (_lock)
            {
                if (!_active || _quizIndex < 0 || _quizIndex >= _currentQuiz.Count)
                    return (false, "No active quiz or no current question.", true);

                var q = _currentQuiz[_quizIndex];
                bool correct = selectedOptionIndex == q.CorrectOptionIndex;
                if (correct) _quizScore++;

                string feedback = correct ? "Correct! " : "Incorrect. ";
                if (!string.IsNullOrWhiteSpace(q.Explanation))
                    feedback += q.Explanation + " ";
                feedback += $"Score: {_quizScore}/{_quizIndex + 1}.";

                _quizIndex++;
                bool finished = _quizIndex >= _currentQuiz.Count;
                if (finished)
                {
                    _active = false;
                    feedback += $" Quiz finished. Final score: {_quizScore}/{_currentQuiz.Count}. {GetScoreMessage(_quizScore, _currentQuiz.Count)}";
                }
                else
                {
                    feedback += $" Next question: {_quizIndex + 1}/{_currentQuiz.Count}.";
                }

                return (correct, feedback, finished);
            }
        }

        // Indicates whether a quiz session is in progress
        public bool IsActive
        {
            get
            {
                lock (_lock) { return _active; }
            }
        }

        // Final summary when quiz finished (null while active)
        public string? GetFinalSummary()
        {
            lock (_lock)
            {
                if (_active) return null;
                if (_currentQuiz.Count == 0) return null;
                return $"Final score: {_quizScore}/{_currentQuiz.Count}. {GetScoreMessage(_quizScore, _currentQuiz.Count)}";
            }
        }

        // Human-friendly message based on percent score
        public string GetScoreMessage(int score, int total)
        {
            if (total == 0) return string.Empty;
            var pct = (double)score / total;
            if (pct == 1.0) return "Perfect score — excellent work!";
            if (pct >= 0.8) return "Great job — you clearly know your stuff.";
            if (pct >= 0.5) return "Not bad — a little review will help solidify these topics.";
            return "Keep learning — practice and review will improve your score.";
        }

        // --- Internal: build a default question bank (12+ questions) ---
        private List<QuizQuestion> CreateDefaultBank()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion("What does MFA stand for?", new[] { "Multi-Factor Authentication", "Managed Firewall Access", "Multi-Form Authorization", "Message Format Authentication" }, 0, "MFA stands for Multi-Factor Authentication."),
                new QuizQuestion("Which protocol indicates a secure website connection?", new[] { "HTTP", "FTP", "HTTPS", "SMTP" }, 2, "HTTPS is HTTP over TLS/SSL and indicates an encrypted connection."),
                new QuizQuestion("What is a common sign of a phishing email?", new[] { "Unexpected urgency", "Perfect grammar", "Known sender address", "Personalized account details" }, 0, "Phishing often uses urgent language to trick users into acting quickly."),
                new QuizQuestion("Which is the best practice for passwords?", new[] { "Reuse one strong password", "Use short memorable passwords", "Use unique long passwords and a password manager", "Write them on a sticky note" }, 2, "Use unique, long passwords and consider a reputable password manager."),
                new QuizQuestion("Ransomware primarily:", new[] { "Steals credentials silently", "Encrypts files and demands payment", "Monitors network traffic only", "Improves system performance" }, 1, "Ransomware encrypts files and demands payment for the decryption key."),
                new QuizQuestion("What is social engineering?", new[] { "A software engineering discipline", "Technique that manipulates people into giving information", "Hardware tampering", "Network segmentation" }, 1, "Social engineering manipulates people rather than exploiting software vulnerabilities."),
                new QuizQuestion("Which is the safest way to receive MFA codes?", new[] { "SMS messages", "Email only", "Authenticator app or hardware token", "Post-it note" }, 2, "Authenticator apps or hardware tokens are stronger than SMS for MFA."),
                new QuizQuestion("A trusted certificate for a website is issued by:", new[] { "A Certificate Authority (CA)", "The website owner without oversight", "An email provider", "A DNS server" }, 0, "Certificate Authorities (CAs) issue trusted TLS certificates."),
                new QuizQuestion("What does 'least privilege' mean?", new[] { "Granting full rights to everyone", "Giving users only the access they need", "Removing all permissions", "Allowing admin access by default" }, 1, "Least privilege gives users only the permissions necessary to perform their tasks."),
                new QuizQuestion("Which action reduces the risk from software vulnerabilities?", new[] { "Never update software", "Apply security patches promptly", "Disable backups", "Use default passwords" }, 1, "Applying security patches promptly reduces exposure to known vulnerabilities."),
                new QuizQuestion("What is the purpose of a firewall?", new[] { "Physically protect the server", "Filter and control network traffic", "Encrypt files at rest", "Scan for malware on a mobile device" }, 1, "Firewalls filter and control network traffic according to policy."),
                new QuizQuestion("What is a strong indicator of a scam website?", new[] { "EV SSL certificate", "Too-good-to-be-true offers and unknown payment methods", "Clear contact information", "Consistent branding" }, 1, "Scam sites often offer unrealistic deals and ask for unusual payment methods."),
                // extra questions (optional)
                new QuizQuestion("Which practice improves web privacy for users?", new[] { "Enabling third-party cookies", "Using private browsing + tracker blockers", "Installing unknown extensions", "Disabling HTTPS" }, 1, "Private browsing and tracker blockers help reduce cross-site tracking."),
                new QuizQuestion("What does 'pharming' refer to?", new[] { "Hijacking DNS to redirect traffic to malicious sites", "A type of firewall", "Encrypting files for ransom", "A social-engineering phone scam" }, 0, "Pharming redirects legitimate traffic to malicious sites, often by DNS compromise.")
            };
        }
    }
}