using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Alexa.NET;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;




// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TodayILearnedAlexaSkillCSharp
{
    public class Function
    {
        private string g_title { get; set; }
        private string g_score { get; set; }
        private string g_lastupdated { get; set; }


        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;
            IOutputSpeech innerResponse = null;
            var log = context.Logger;

            //Intents
            //Launch Request
            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made: 'Alexa, open Today I Learned");
                innerResponse = new PlainTextOutputSpeech();
                (innerResponse as PlainTextOutputSpeech).Text = Messages.WelcomeMessage;
                response.Response.ShouldEndSession = false;
            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                switch (intentRequest.Intent.Name)
                {
                    case "AMAZON.CancelIntent":
                        log.LogLine($"AMAZON.CancelIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = Messages.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.StopIntent":
                        log.LogLine($"AMAZON.StopIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = Messages.StopMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.HelpIntent":
                        log.LogLine($"AMAZON.HelpIntent: send HelpMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = Messages.HelpMessage + Messages.giveItATry;
                        response.Response.ShouldEndSession = false;
                        break;
                    case "AMAZON.RepeatIntent":
                        log.LogLine($"AMAZON.RepeatIntent: send repeat");
                        innerResponse = new PlainTextOutputSpeech();
                        if (!string.IsNullOrEmpty(this.g_title))
                            (innerResponse as PlainTextOutputSpeech).Text = this.g_title + "\n" + Messages.repromptSpeech;
                        else
                            (innerResponse as PlainTextOutputSpeech).Text = "No available T.I.L to repeat. " + Messages.repromptSpeech;
                        response.Response.ShouldEndSession = false;
                        break;
                    //Main Intents
                    case "TodayILearned":
                        log.LogLine($"TodayILearned: send TIL");
                        GetRandomTIL til = new GetRandomTIL("reddit_til", context);
                        GetRandomTIL.Data.data til_child = await til.Child();
                        log.LogLine(string.Format("TodayILearned: received TIL: \nid: {0}\ntitle: {1}\nscore: {2}\nlast_updated: {3}", til_child.id, til_child.title, til_child.score, til_child.last_updated));

                        //Parsing the title
                        string title = til_child.title;
                        if (title.StartsWith("TIL") ||
                            title.StartsWith("TiL") ||
                            title.StartsWith("Til") ||
                            title.StartsWith("til") ||
                            title.StartsWith("tIL") ||
                            title.StartsWith("tIl") ||
                            title.StartsWith("TIl"))
                        {
                            title = title.Remove(0, 3).TrimStart();
                        }
                        title = parseTitle(title, log);

                        //Global variables
                        this.g_title = Messages.tilresponse + title;
                        this.g_score = til_child.score.ToString();
                        this.g_lastupdated = til_child.last_updated.Substring(0, 10);

                        //What to say
                        string say = Messages.tilresponse + title + ". \n" + Messages.repromptSpeech;
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = say;

                        //Build response
                        var finalResponse = ResponseBuilder.TellWithCard(innerResponse, "Today I Learned",
                            string.Format("{0}\n\nScore: {1}\nLast Updated: {2}", censorTitle(title), this.g_score, this.g_lastupdated));
                        finalResponse.Response.ShouldEndSession = false;
                        
                        //Response
                        return finalResponse;
                    case "TodayILearnedScore":
                        log.LogLine($"TodayILearnedScore: send TIL score");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = string.Format("The score of the T.I.L is {0}.\n{1}", this.g_score, Messages.repromptSpeech);
                        response.Response.ShouldEndSession = false;
                        break;
                    //End - Main Intents
                    case "Unhandled:":
                        log.LogLine($"Unhandled: send HelpMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = Messages.HelpMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                    default:
                        log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = Messages.HelpMessage;
                        response.Response.ShouldEndSession = true;
                        break;
                }
            }

            response.Response.OutputSpeech = innerResponse;
            response.Version = "1.0";
            return response;
        }

        public string parseTitle(string t, ILambdaLogger log)
        {
            try
            {
                char first = t[0];
                if (!Char.IsLetter(first))
                    t = t.Remove(0, 1);
                if (t.Contains("Today I Learned"))
                    t = t.Replace("Today I Learned", "");
                else if (t.Contains("today I learned"))
                    t = t.Replace("Today I Learned", "");
                //Optimize the beginning
                string f = t[0].ToString();
                if (f == f.ToLower() && f != f.ToUpper())
                    t = t[0].ToString().ToUpper() + t.Substring(1, t.Length - 1);
                return t;
            }
            catch (Exception e)
            {
                log.LogLine("error: " + e.Message);
                return t;
            }
        }
        public string censorTitle(string t)
        {
            if (t.Contains("fucking"))
                t = t.Replace("fucking", "f****ing");
            else if (t.Contains("cunt"))
                t = t.Replace("cunt", "c**t");
            return t;
        }


        //All messages
        private static class Messages
        {
            public static string WelcomeMessage { get { return "Wecome to Today I Learned Alexa Skill! Would you like to hear a Today I Learned fact or T.I.L for short?"; } }
            public static string StopMessage { get { return "Okay! But remember, knowledge is power!"; } }
            public static string HelpMessage { get { return "Today I Learned is a skill based on facts and historical references that are just as educational as they are interesting and funny. " +
                                                    "You receive a skill card with every fact so you can review it or even send it to a friend. " +
                                                    "You can use this skill by saying Alexa, ask Today I Learned for a fact. " +
                                                    "Also you can ask for a score of current T.I.L by saying what is the score."; } }
            public static string giveItATry {  get { return " Would you like to give it a try?"; } }
            public static string tilresponse { get { return "Today you learned! "; } }
            public static string repromptSpeech { get { return "Would you like to hear another T.I.L?"; } }
        }

    }
}
