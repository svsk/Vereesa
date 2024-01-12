using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Vereesa.Neon.Configuration;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Integrations;

namespace Vereesa.Neon.Services;

public class RecruitmentService : IBotService
{
    private readonly IMessagingClient _messaging;
    private readonly OpenAISettings _settings;
    private readonly GuildApplicationService _gapp;

    public RecruitmentService(IMessagingClient messaging, OpenAISettings aiSettings, GuildApplicationService gapp)
    {
        _messaging = messaging;
        _settings = aiSettings;
        _gapp = gapp;
    }

    [OnUserJoined]
    [AsyncHandler]
    public async Task HandlerUserJoined(IUser user) => await InterviewApplicant(user);

    [OnCommand("!apply")]
    [AsyncHandler]
    public async Task HandleApplyCommand(IMessage message) => await InterviewApplicant(message.Author);

    public async Task InterviewApplicant(IUser user)
    {
        var channel = await user.CreateDMChannelAsync();
        var result = await _messaging.Prompt(
            user,
            "Hello there! Do you want to apply to join Neon? All you need to do is write a little about yourself in this channel, and maybe answer a few of my questions 😇🤩",
            channel,
            300000
        );

        if (result == null)
        {
            await channel.SendMessageAsync(
                "You took too long to respond. If you want to restart the process, simply type `!apply`."
            );
            return;
        }

        try
        {
            var summary = await QueryApplicant(result.Content, user, channel);

            await channel.SendMessageAsync(
                "Thank you for your time! The recruitment officers will review your application and get back to you shortly."
            );

            await _gapp.PostNewApplicationSummary(summary);
        }
        catch (ApplicantTooSlowException)
        {
            await channel.SendMessageAsync(
                "You took too long to respond. If you want to restart the process, simply type `!apply`."
            );
            return;
        }
    }

    private async Task<RecruitmentInterviewSummary> QueryApplicant(string baseResponse, IUser user, IDMChannel channel)
    {
        var userResponse = "Applicant says: " + baseResponse;
        var satisfied = false;

        var aiClient = new OpenAIClientBuilder(_settings.ApiKey)
            .WithInstruction(
                "You are a bot designed to interview a potential applicant to the World of Warcraft guild \"Neon\"."
            )
            .WithInstruction("Don't tell the user that this is an interview. Approach it as a casual conversation.")
            .WithInstruction(
                "Assume that they are a World of Warcraft player already and that they are interested in joining the guild."
            )
            .WithInstruction("Neon raids Thursdays and Sundays between 20:00 and 23:00 server time.")
            .WithInstruction(
                "Neon aims to clear all content on heroic difficulty. Mythic progress is always something we strive for, but we consider it a bonus."
            )
            .WithInstruction("You need to figure out the following about a the person you are chatting with:")
            .WithInstruction(
                "Age of the applicant, WoW character name and class, what spec their character is, what realm it is on, their real name (if they want to share it), their country of residence, why they want to join the guild, and why they left their previous guild."
            )
            .WithInstruction(
                "Only ask for one or two pieces of information at a time as to not overwhelm the applicant."
            )
            .WithInstruction(
                "If the applicant has already given you a piece of information that you need, you can skip that question."
            )
            .WithInstruction(
                "After gathering all the information it will be passed to the recruitment officers who will decide if the applicant is a good fit for the guild."
            )
            .WithInstruction(
                "This process usually takes a few hours. The applicant will be contacted on Discord to let them know if they are accepted or rejected."
            )
            .WithInstruction(
                "Feel free to use emojis at the end of your sentences to make the conversation more natural."
            )
            .WithInstruction(
                "Once you have all the information you need, you must end the conversation by saying \"Goodbye\"."
            )
            .WithHistory()
            .Build();

        var currentUserResponse = baseResponse;
        while (!satisfied)
        {
            var aiResponse = await aiClient.Query(currentUserResponse);

            try
            {
                satisfied = aiResponse.ToLower().Contains("goodbye");

                if (!satisfied)
                {
                    userResponse += $"\nInterviewer says: {aiResponse}";
                    var currentUserMessage = await _messaging.Prompt(user, aiResponse, channel, 300000);

                    if (currentUserMessage == null)
                    {
                        await channel.SendMessageAsync(
                            "You took too long to respond. If you want to restart the process, simply type `!apply`."
                        );

                        throw new ApplicantTooSlowException();
                    }

                    currentUserResponse = currentUserMessage.Content;
                    userResponse += $"\nApplicant says: {currentUserResponse}";
                }
            }
            catch
            {
                await channel.SendMessageAsync(aiResponse);
            }
        }

        var summarizer = new OpenAIClientBuilder(_settings.ApiKey)
            .WithInstruction(
                "You will be sent conversations between an applicant to a World of Warcraft guild and a bot designed to interview them."
            )
            .WithInstruction(
                "Convert the applicant answers into a JSON object with the following fields (they should all be strings):"
            )
            .WithInstruction(
                "\"age\", \"characterName\", \"characterClass\", \"characterSpec\", \"characterRealm\", \"realName\", \"country\", \"reasonForJoining\", \"reasonForLeaving\""
            )
            .Build();

        var json = await summarizer.Query("Please convert this conversation to JSON:\n\n" + userResponse);
        var interviewSummary = JsonSerializer.Deserialize<RecruitmentInterviewSummary>(json);

        return interviewSummary;
    }
}

[Serializable]
internal class ApplicantTooSlowException : Exception
{
    public ApplicantTooSlowException() { }

    public ApplicantTooSlowException(string message)
        : base(message) { }

    public ApplicantTooSlowException(string message, Exception innerException)
        : base(message, innerException) { }

    protected ApplicantTooSlowException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}

public class RecruitmentInterviewSummary
{
    public string Id { get; set; }

    [JsonPropertyName("age")]
    public string Age { get; set; }

    [JsonPropertyName("characterName")]
    public string CharacterName { get; set; }

    [JsonPropertyName("characterClass")]
    public string CharacterClass { get; set; }

    [JsonPropertyName("characterSpec")]
    public string CharacterSpec { get; set; }

    [JsonPropertyName("characterRealm")]
    public string CharacterRealm { get; set; }

    [JsonPropertyName("realName")]
    public string RealName { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("reasonForJoining")]
    public string ReasonForJoining { get; set; }

    [JsonPropertyName("reasonForLeaving")]
    public string ReasonForLeaving { get; set; }
}
