using System.Collections.Generic;
using System.Linq;

namespace Vereesa.Neon.Data.Models.NeonApi
{
    public class Application
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public List<ApplicationQuestion> ApplicationQuestions { get; set; }
        public string CurrentStatusString { get; set; }

        public string GetFirstAnswerByQuestionPart(string questionPart)
        {
            questionPart = questionPart.ToLowerInvariant();

            foreach (var applicationQuestion in this.ApplicationQuestions)
            {
                if (applicationQuestion.Question.ToLowerInvariant().Contains(questionPart))
                {
                    return applicationQuestion.Answer;
                }
            }

            return null;
        }

        public string GetFirstAnswerByEitherQuestionPart(params string[] questionParts)
        {
            var invariantQuestionParts = questionParts.Select(qp => qp.ToLowerInvariant()).ToList();

            foreach (var applicationQuestion in this.ApplicationQuestions)
            {
                foreach (var questionPart in invariantQuestionParts)
                {
                    if (applicationQuestion.Question.ToLowerInvariant().Contains(questionPart))
                    {
                        return applicationQuestion.Answer;
                    }
                }
            }

            return null;
        }
    }
}
