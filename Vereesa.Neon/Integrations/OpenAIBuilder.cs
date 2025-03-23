using System.Text.Json;
using OpenAI;
using OpenAI.Chat;

namespace Vereesa.Neon.Integrations;

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
        return new BuiltOpenAIClient(client, _instructions, _shouldRememberHistory, _apiKey);
    }
}

public class BuiltOpenAIClient
{
    private readonly ChatClient _client;
    private readonly List<string> _instructions;
    private readonly bool _shouldRememberHistory;
    private readonly string _apiKey;
    private List<ChatMessage> _chatHistory = new List<ChatMessage>();

    public BuiltOpenAIClient(OpenAIClient client, List<string> instructions, bool shouldRememberHistory, string apiKey)
    {
        _client = client.GetChatClient("gpt-4o-mini");
        _instructions = instructions;
        _shouldRememberHistory = shouldRememberHistory;
        _apiKey = apiKey;
    }

    public async Task<string?> Query(string query)
    {
        var options = new ChatCompletionOptions { Temperature = 0f };

        var messages = new List<ChatMessage> { new SystemChatMessage(string.Join(" ", _instructions)) };
        messages.AddRange(_chatHistory);

        messages.Add(new UserChatMessage(query));

        var result = await _client.CompleteChatAsync(messages, options);
        var resultMessage = result.Value.Content.FirstOrDefault()?.Text;

        if (_shouldRememberHistory)
        {
            _chatHistory.Add(new UserChatMessage(query));
            _chatHistory.Add(new AssistantChatMessage(resultMessage));
        }

        return resultMessage;
    }

    public async Task<T?> QueryAs<T>(string query)
    {
        var resultMessage = await Query(query);
        if (resultMessage == null)
        {
            throw new Exception("OpenAI returned null");
        }

        return JsonSerializer.Deserialize<T>(resultMessage);
    }
}
