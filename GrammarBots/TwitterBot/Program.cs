using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TwitterBot
{
    public class Program
    {
        public class Mistake
        {
            public string TwitterSearch { get; set; }
            public string RegexPattern { get; set; }
            public string RegexReplace { get; set; }
            public string Explanation { get; set; }
        }

        public static Mistake HaveCame = new Mistake
        {
            TwitterSearch = @"""have came""",
            RegexPattern = @"(.*\b)have came(\b.*)",
            RegexReplace = null, // @"$1**have come**$2",
            Explanation = @"It's **have come**, not **have came**, thanks! See http://www.english-test.net/forum/ftopic23316.html"
        };

        public static Mistake ALot = new Mistake
        {
            TwitterSearch = @"""alot""",
            RegexPattern = @"(.*\b)alot(\b.*)",
            RegexReplace = null,
            Explanation = @"It's **a lot**, not **alot**. ""Alot"" isn't a word. Thanks!"
        };

        public static Mistake OfInsteadOfHave = new Mistake
        {
            TwitterSearch = @"""should of"" OR ""would of"" OR ""could of"" OR ""might of"" OR ""must of""",
            RegexPattern = @"(?:^|\s+)(((c|sh|w)ould\s+)of)(?:(?:\s+[^c])|\.|$)",
            RegexReplace = null,
            Explanation = @"Should have, would have, could have, might have, must have. Have, not of. Thanks!"
        };

        public static Mistake Definately = new Mistake
        {
            TwitterSearch = "definately",
            RegexPattern = @"(?:^|\s+)(definately)(?:(?:\s+)|\.|$)",
            RegexReplace = null,
            Explanation = @"It's not ""definately"", it's ""definitely"", with an ""i"" and no ""a"", thanks! See http://www.d-e-f-i-n-i-t-e-l-y.com."
        };

        static Mistake[] Mistakes = { HaveCame, ALot, OfInsteadOfHave, Definately };

        static void Main(string[] args)
        {
            string consumerKey = Environment.GetEnvironmentVariable("Grammar_FTFY_consumerKey");
            string consumerSecret = Environment.GetEnvironmentVariable("Grammar_FTFY_consumerSecret");
            string accessToken = Environment.GetEnvironmentVariable("Grammar_FTFY_accessToken");
            string accessTokenSecret = Environment.GetEnvironmentVariable("Grammar_FTFY_accessTokenSecret");

            var service = new Budgie.TwitterClient(consumerKey, consumerSecret);
            service.Authenticate(accessToken, accessTokenSecret);

            service.GetUserTimelineAsync().ContinueWith(t =>
            {
                var lastTweet = t.Result.Result.First().Id;

                while (true)
                {
                    foreach (var mistake in Mistakes)
                    {
                        // Need to avoid a closure error where mistake gets modified before the async callback continues
                        var mistakeLocal = mistake;

                        service.SearchAsync(mistakeLocal.TwitterSearch, 1, lastTweet + 1).ContinueWith(s =>
                        {
                            if (s.Result.Result.Any())
                            {
                                var tweet = s.Result.Result.First();
                                lastTweet = tweet.Id;

                                var reply = "@" + tweet.User.ScreenName + " " + mistakeLocal.Explanation;

                                if (mistakeLocal.RegexReplace != null)
                                {
                                    var correction = Regex.Replace(tweet.Text, mistakeLocal.RegexPattern, mistakeLocal.RegexReplace, RegexOptions.IgnoreCase);

                                    if (correction != tweet.Text)
                                    {
                                        {
                                            var mtPrefix = "MT @" + tweet.User.ScreenName + " ";

                                            if (mtPrefix.Length + correction.Length <= 140)
                                            {
                                                reply = mtPrefix + correction;
                                            }
                                        }
                                    }
                                }

                                service.ReplyToAsync(tweet.Id, reply).ContinueWith(r =>
                                {
                                    if (r.Result.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        Console.WriteLine(tweet.CreatedAt.ToShortTimeString());
                                        Console.WriteLine("@" + tweet.User.ScreenName + " " + tweet.Text);
                                        Console.WriteLine("    " + reply);
                                        Console.WriteLine(tweet.Id);
                                        Console.WriteLine();

                                        // Look beyond our own tweet
                                        lastTweet = r.Result.Result.Id;

                                        // Follow the user we just corrected
                                        service.FollowAsync(tweet.User.ScreenName);

                                        // Wait a bit before we correct anyone else
                                        System.Threading.Thread.Sleep(TimeSpan.FromMinutes(30));
                                    }
                                }).Wait();
                            }
                        }).Wait();
                    }
                }
            }).Wait();
        }
    }
}
