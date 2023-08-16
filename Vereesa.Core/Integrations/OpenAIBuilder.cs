using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.AI.OpenAI;

namespace Vereesa.Core.Integrations;

public class OpenAIClientBuilder
{
    private string _apiKey;
    private List<string> _instructions = new List<string>();
    private bool _shouldRememberHistory = false;

    public OpenAIClientBuilder(string apiKey)
    {
        _apiKey = apiKey;
    }

    public OpenAIClientBuilder WithInstruction(string instruction)
    {
        _instructions.Add(instruction);
        return this;
    }

    public OpenAIClientBuilder WithHistory()
    {
        _shouldRememberHistory = true;
        return this;
    }

    public BuiltOpenAIClient Build()
    {
        var client = new OpenAIClient(_apiKey);
        return new BuiltOpenAIClient(client, _instructions, _shouldRememberHistory);
    }
}

public class BuiltOpenAIClient
{
    private readonly OpenAIClient _client;
    private readonly List<string> _instructions;
    private readonly bool _shouldRememberHistory;
    private List<ChatMessage> _chatHistory = new List<ChatMessage>();

    public BuiltOpenAIClient(OpenAIClient client, List<string> instructions, bool shouldRememberHistory)
    {
        _client = client;
        _instructions = instructions;
        _shouldRememberHistory = shouldRememberHistory;
    }

    public async Task<string> Query(string query)
    {
        var options = new ChatCompletionsOptions { Temperature = 0f };
        options.Messages.Add(new ChatMessage(ChatRole.System, string.Join(" ", _instructions)));
        foreach (var msg in _chatHistory)
            options.Messages.Add(msg);

        options.Messages.Add(new ChatMessage(ChatRole.User, query));

        var result = await _client.GetChatCompletionsAsync("gpt-3.5-turbo", options);
        var resultMessage = result.Value.Choices.FirstOrDefault()?.Message?.Content;

        if (_shouldRememberHistory)
        {
            _chatHistory.Add(new ChatMessage(ChatRole.User, query));
            _chatHistory.Add(new ChatMessage(ChatRole.Assistant, resultMessage));
        }

        return resultMessage;
    }

    public async Task<T> QueryAs<T>(string query)
    {
        var resultMessage = await Query(query);
        return JsonSerializer.Deserialize<T>(resultMessage);
    }
}
