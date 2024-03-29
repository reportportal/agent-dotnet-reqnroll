﻿using ReportPortal.Client.Abstractions;
using ReportPortal.Shared.Configuration;

namespace ReportPortal.ReqnrollPlugin.EventArguments
{
    public class InitializingEventArgs
    {
        public InitializingEventArgs(IConfiguration config)
        {
            Config = config;
        }

        public IConfiguration Config { get; set; }

        public IClientService Service { get; set; }
    }
}
