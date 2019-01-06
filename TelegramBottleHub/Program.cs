using Microsoft.Extensions.Configuration;
using System.Threading;

namespace TelegramBottleHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var configuration = builder.Build();

            Hub.Configure(configuration);
            Hub.Instance.Start();

            Thread.Sleep(int.MaxValue);
        }
    }
}
