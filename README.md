# XPlat.VUI
A simple cross-platform library for building VUI smart speaker skills using C#.

# Supported platform
* Google Assistant
* Amazon Alexa
* LINE Clova

# Usage
## Inherit AssistantBase abstract class

Create a class that inherits `AssistantBase`.

```csharp
public class MyAssistant : AssistantBase
{
}
```

*If you need more properties or methods, use an extended interface.

```csharp
public interface ILoggableAssistant : IAssistant
{
    ILogger Logger { get; set; }
}

public class MyAssistant : AssistantBase, ILoggableAssistant
{
    public ILogger Logger { get; set; }
}
```

## Instantiate or Dependency Injection

Instantiate the derived class.

```csharp
var Assistant = new MyAssistant();
```

If you want to use Dependency Injection, call `AddAssistant` extension method in the Startup class.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAssistant<IAssistant, MyAssistant>();
    services.AddMvc();
}
```

## Call RespondAsync method

Pass request object to handle request and create response.

```csharp
var response = await Assistant.RespondAsync(Request);
return new OkObjectResult(response);
```

## Override Methods

Override methods executed for each request type or event.

```csharp
public class MyAssistant : AssistantBase
{
    protected override Task OnLaunchRequestAsync(
        Dictionary<string, object> session, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task OnIntentRequestAsync(
        string intent, Dictionary<string, object> slots, Dictionary<string, object> session,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

## Add content to Response property

You can add content for response of Assistant Extension to `Response` property with method chaining.

When you add the text, you can also set the language to overwrite default language.

1\. Add Reply. 

```csharp
Response
    .Speak("お元気ですか？")
    .Play("https://dummy.domain/myaudio.mp3");
```

2\. Keep listening for multi-turn session and add reprompt.

```csharp
Response
    .Speak("お元気ですか？")
    .KeepListening("元気かどうか教えてください。");
```

3\. Different responce for each platform

```csharp
Response
    .Speak("私はグーグルです。", Platform.GoogleAssistant)
    .Speak("私はアレクサです。", Platform.Alexa)
    .Speak("私はクローバです。", Platform.Clova);
```

4\. Different processing for each platform

```csharp
if (Request.OriginalClovaRequest != null)
{
    // Push LINE Message to same account
    await lineMessagingClient.PushMessageAsync(
        Request.OriginalClovaRequest.Session.User.UserId, "Hello!");        
}
```

# LISENCE

[MIT](./LICENSE)
