using Microsoft.Extensions.Configuration;
using System;

namespace TelegramBottleHub.General.Managers.Core
{
    public abstract class ManagerCore : IDisposable
    {
        protected readonly IConfigurationRoot Configuration;

        protected ManagerCore(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }

        public abstract void Dispose();
    }
}
