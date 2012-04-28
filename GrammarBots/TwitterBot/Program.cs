using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TwitterBot
{
    class Program
    {
        static void Main(string[] args)
        {
            TweetSharp.TwitterService service = new TweetSharp.TwitterService();

            string consumerKey = Environment.GetEnvironmentVariable("Grammar_FTFY_consumerKey");
            string consumerSecret = Environment.GetEnvironmentVariable("Grammar_FTFY_consumerSecret");
            string token = Environment.GetEnvironmentVariable("Grammar_FTFY_accessToken");
            string tokenSecret = Environment.GetEnvironmentVariable("Grammar_FTFY_accessTokenSecret");

            service.AuthenticateWith(consumerKey, consumerSecret, token, tokenSecret);

            long lastTweet = service.ListTweetsOnUserTimeline().FirstOrDefault().Id;

            while (true)
            {
                var haveCame = service.SearchSince(lastTweet + 1, "\" have came \"");

                if (haveCame.Statuses.Any())
                {
                    lastTweet = haveCame.Statuses.First().Id;
                }

                foreach (var haveCameMistake in haveCame.Statuses)
                {
                    var correction = Regex.Replace(haveCameMistake.Text, " have came ", " have **COME** ", RegexOptions.IgnoreCase);

                    if (correction != haveCameMistake.Text)
                    {
                        var mtPrefix = "MT @" + haveCameMistake.Author.ScreenName + " ";

                        string reply;
                        if (mtPrefix.Length + correction.Length > 140)
                        {
                            reply = "@" + haveCameMistake.Author.ScreenName + " It's 'have **COME**', not 'have CAME.'";
                        }
                        else
                        {
                            reply = mtPrefix + correction;
                        }

                        var tweet = service.SendTweet(reply, haveCameMistake.Id);

                        Console.WriteLine(haveCameMistake.CreatedDate.ToShortTimeString());
                        Console.WriteLine("@" + haveCameMistake.Author.ScreenName + " " + haveCameMistake.Text);
                        Console.WriteLine("    " + reply);
                        Console.WriteLine(tweet.Id);
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
