using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime;

namespace Vereesa.Data.Models.NeonApi
{
    public class Application
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public IList<ApplicationQuestion> ApplicationQuestions { get; set; }

        public string CurrentStatusString { get; set; }
        
        public DateTime GetCreatedDateUtc(string timestampTimeZone)
        {            
            var timestamp = this.GetFirstAnswerByQuestionPart("Timestamp");

            if (string.IsNullOrEmpty(timestamp))
            {
                return DateTime.UtcNow;
            }

            var parsedDateTime = DateTime.ParseExact(timestamp, "M/d/yyyy H:mm:ss", null);
            
            var localDateTime = LocalDateTime.FromDateTime(parsedDateTime);
            var orignatingTimeZone = DateTimeZoneProviders.Tzdb[timestampTimeZone]; //The "DB" (aka the Google Sheet) providing the values is set up with Europe/Berlin time...
            var zonedDateTime = orignatingTimeZone.AtStrictly(localDateTime);

            return zonedDateTime.ToDateTimeUtc();
        }

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