using Alexa.NET.Response;
using Alexa.NET.Response.Directive;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf.WellKnownTypes;
using LineDC.CEK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XPlat.VUI.Models
{
    public class AssistantResponse
    {
        private List<OutputObject> OutputObjects { get; } = new List<OutputObject>();
        private List<AudioItemObject> AudioItemObjects { get; } = new List<AudioItemObject>();
        private bool ShouldEndSession { get; set; } = true;
        private string RepromptText { get; set; }
        internal string UserId { get; set; }

        public AssistantResponse Speak(string text, Platform targetPlatform = Platform.All)
        {
            OutputObjects.Add(new OutputObject { TargetPlatform = targetPlatform, Type = OutputType.Text, Value = text });
            return this;
        }

        public AssistantResponse Play(string url, Platform targetPlatform = Platform.All)
        {
            OutputObjects.Add(new OutputObject { TargetPlatform = targetPlatform, Type = OutputType.Url, Value = url });
            return this;
        }

        public AssistantResponse Break(int second, Platform targetPlatform = Platform.All)
        {
            if (second == 0 || second > 10)
            {
                throw new Exception("Break time must be set between 1 and 10 seconds.");
            }

            OutputObjects.Add(new OutputObject { TargetPlatform = targetPlatform, Type = OutputType.Break, BreakTime = second });
            return this;
        }

        public AssistantResponse PlayWithAudioPlayer(
            string audioItemId, string url, string title, string subTitle,
            string previousAudioId = null, AudioPlayBehavior behavior = AudioPlayBehavior.ReplaceAll,
            Platform targetPlatform = Platform.All)
        {
            AudioItemObjects.Add(new AudioItemObject
            {
                AudioItemId = audioItemId, TargetPlatform = targetPlatform, Url = url, Title = title, SubTitle = subTitle,
                PreviousAudioItemId = previousAudioId, AudioPlayBehavior = behavior
            });
            return this;
        }

        public void KeepListening(string reprompt = null)
        {
            if (reprompt != null)
            {
                RepromptText = reprompt;
            }
            ShouldEndSession = false;
        }

        public string ToGoogleAssistantResponse()
        {
            var ssml = GetSsmlResponse(Platform.GoogleAssistant);
            var webhookResponse = new WebhookResponse
            {
                Payload = new Struct
                {
                    Fields =
                    {
                        ["google"] = Value.ForStruct(new Struct
                        {
                            Fields =
                            {
                                ["expectUserResponse"] = Value.ForBool(!ShouldEndSession),
                                ["userStorage"] = Value.ForString($"{{ \"userId\": \"{UserId}\" }}"),
                                ["resetUserStorage"] = Value.ForBool(true)
                            }
                        })
                    }
                }
            };

            // Media response for audio
            if (AudioItemObjects.Any())
            {
                webhookResponse.Payload.Fields["google"].StructValue.Fields.Add("richResponse", Value.ForStruct(new Struct
                {
                    Fields =
                    {
                        ["items"] = Value.ForList(
                            Value.ForStruct(new Struct
                            {
                                Fields =
                                {
                                    ["simpleResponse"] = Value.ForStruct(new Struct
                                    {
                                        Fields =
                                        {
                                            ["ssml"] = Value.ForString(ssml)
                                        }
                                    })
                                }
                            }),
                            Value.ForStruct(new Struct
                            {
                                Fields =
                                {
                                    ["mediaResponse"] = Value.ForStruct(new Struct
                                    {
                                        Fields =
                                        {
                                            ["mediaType"] = Value.ForString("AUDIO"),
                                            ["mediaObjects"] = Value.ForList(AudioItemObjects.Select(audio => Value.ForStruct(new Struct
                                            {
                                                Fields =
                                                {
                                                    ["contentUrl"] = Value.ForString(audio.Url),
                                                    ["description"] = Value.ForString(audio.SubTitle),
                                                    ["icon"] = Value.ForStruct(new Struct
                                                    {
                                                        Fields =
                                                        {
                                                            ["url"] = Value.ForString(audio.ImageUrl ?? "http://storage.googleapis.com/automotive-media/album_art.jpg"),
                                                            ["accessibilityText"] = Value.ForString(audio.Title)
                                                        }
                                                    }),
                                                    ["name"] = Value.ForString(audio.Title)
                                                }
                                            })).ToArray())
                                        }
                                    })
                                }
                            })),
                    }
                }));

                if (!ShouldEndSession)
                {
                    webhookResponse.Payload.Fields["google"].StructValue.Fields["richResponse"].StructValue.Fields.Add("suggestions", Value.ForList(
                        Value.ForStruct(new Struct
                        {
                            Fields =
                            {
                                ["title"] = Value.ForString("Stop")
                            }
                        })));
                }
            }
            else
            {
                // normal
                webhookResponse.FulfillmentText = ssml;
            }

            return webhookResponse.ToString();
        }

        public SkillResponse ToAlexaResponse()
        {
            if (AudioItemObjects.Any(obj => obj.TargetPlatform == Platform.All || obj.TargetPlatform == Platform.Alexa))
            {
                // AudioPlayer
                var response = new SkillResponse
                {
                    Response = new ResponseBody
                    {
                        ShouldEndSession = ShouldEndSession,
                        Directives = new List<IDirective>()
                    },
                    Version = "1.0"
                };

                if (OutputObjects.Any(obj => obj.TargetPlatform == Platform.All || obj.TargetPlatform == Platform.Alexa))
                {
                    response.Response.OutputSpeech = new SsmlOutputSpeech
                    {
                        Ssml = GetSsmlResponse(Platform.Alexa)
                    };
                    response.Response.Reprompt = !string.IsNullOrEmpty(RepromptText)
                        ? new Alexa.NET.Response.Reprompt(RepromptText)
                        : null;
                }

                foreach (var audio in AudioItemObjects
                    .Where(audio => audio.TargetPlatform == Platform.All || audio.TargetPlatform == Platform.Alexa))
                {
                    response.Response.Directives.Add(new AudioPlayerPlayDirective
                    {
                        PlayBehavior = audio.AudioPlayBehavior == AudioPlayBehavior.Enqueue
                            ? PlayBehavior.Enqueue
                            : PlayBehavior.ReplaceAll,
                        AudioItem = new Alexa.NET.Response.Directive.AudioItem
                        {
                            Stream = new AudioItemStream
                            {
                                Url = audio.Url,
                                Token = audio.AudioItemId,
                                ExpectedPreviousToken = audio.PreviousAudioItemId
                            },
                            Metadata = new AudioItemMetadata
                            {
                                Title = audio.Title,
                                Subtitle = audio.SubTitle
                            }
                        }
                    });
                }
                return response;
            }
            else if (OutputObjects.Any(obj => obj.TargetPlatform == Platform.All || obj.TargetPlatform == Platform.Alexa))
            {
                // normal
                return new SkillResponse
                {
                    Response = new ResponseBody
                    {
                        OutputSpeech = new SsmlOutputSpeech
                        {
                            Ssml = GetSsmlResponse(Platform.Alexa)
                        },
                        Reprompt = !string.IsNullOrEmpty(RepromptText)
                            ? new Alexa.NET.Response.Reprompt(RepromptText)
                            : null,
                        ShouldEndSession = ShouldEndSession
                    },
                    Version = "1.0"
                };
            }
            else
            {
                // nothing
                return null;
            }
        }

        public CEKResponse ToClovaResponse()
        {
            var response = new CEKResponse();

            // normal
            foreach (var output in OutputObjects
                .Where(output => output.TargetPlatform == Platform.All || output.TargetPlatform == Platform.Clova))
            {
                if (output.Type == OutputType.Text)
                {
                    response.AddText(output.Value);
                }
                else if (output.Type == OutputType.Url)
                {
                    response.AddUrl(output.Value);
                }
                else if (output.Type == OutputType.Break)
                {
                    if (output.BreakTime <= 5)
                    {
                        response.AddUrl($"https://himanago.github.io/silent-mp3/silent_{output.BreakTime}s.mp3");
                    }
                    else
                    {
                        response.AddUrl($"https://himanago.github.io/silent-mp3/silent_5s.mp3");
                        response.AddUrl($"https://himanago.github.io/silent-mp3/silent_{output.BreakTime - 5}s.mp3");
                    }
                }
            }

            // AudioPlayer
            foreach (var audio in AudioItemObjects
                .Where(audio => audio.TargetPlatform == Platform.All || audio.TargetPlatform == Platform.Clova))
            {
                response.Response.Directives.Add(new Directive()
                {
                    Header = new DirectiveHeader()
                    {
                        Namespace = DirectiveHeaderNamespace.AudioPlayer,
                        Name = DirectiveHeaderName.Play
                    },
                    Payload = new AudioPlayPayload
                    {
                        AudioItem = new LineDC.CEK.Models.AudioItem
                        {
                            AudioItemId = audio.AudioItemId,
                            HeaderText = audio.Title,
                            TitleText = audio.Title,
                            TitleSubText1 = audio.SubTitle,
                            ArtImageUrl = audio.ImageUrl,
                            Stream = new AudioStreamInfoObject
                            {
                                BeginAtInMilliseconds = 0,
                                Url = audio.Url,
                                UrlPlayable = true
                            }
                        },
                        PlayBehavior = audio.AudioPlayBehavior == AudioPlayBehavior.Enqueue
                            ? LineDC.CEK.Models.AudioPlayBehavior.ENQUEUE
                            : LineDC.CEK.Models.AudioPlayBehavior.REPLACE_ALL,

                        Source = new Source { Name = audio.Title }
                    }
                });
            }

            if (!ShouldEndSession)
            {
                response.KeepListening();
            }

            return response;
        }

        private string GetSsmlResponse(Platform targetPlatform)
        {
            return $@"<speak>{
                string.Concat(OutputObjects
                    .Where(output => output.TargetPlatform == Platform.All || output.TargetPlatform == targetPlatform)
                    .Select(output =>
                        output.Type switch
                        {
                            OutputType.Text => output.Value,
                            OutputType.Url => $"<audio src=\"{output.Value}\" />",
                            OutputType.Break => $"<break time=\"{output.BreakTime}s\" />",
                            _ => string.Empty
                        }))
            }</speak>";
        }
    }

    internal class OutputObject
    {
        internal Platform TargetPlatform { get; set; }
        internal OutputType Type { get; set; }
        internal string Value { get; set; }
        internal int BreakTime { get; set; }
    }

    internal class AudioItemObject
    {
        internal string AudioItemId { get; set; }
        internal Platform TargetPlatform { get; set; }
        internal string Url { get; set; }
        internal string Title { get; set; }
        internal string SubTitle { get; set; }
        internal string ImageUrl { get; set; }
        internal string PreviousAudioItemId { get; set; }
        internal AudioPlayBehavior AudioPlayBehavior { get; set; }
    }

    internal enum OutputType
    {
        Text,
        Url,
        Break
    }

    public enum AudioPlayBehavior
    {
        ReplaceAll,
        Enqueue
    }
}
