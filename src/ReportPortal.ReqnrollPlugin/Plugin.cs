using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.ReqnrollPlugin;
using Reqnroll.Bindings;
using Reqnroll.Infrastructure;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;
using System;
using System.IO;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.ReqnrollPlugin
{
    /// <summary>
    /// Registered Reqnroll plugin from configuration file.
    /// </summary>
    internal class Plugin : IRuntimePlugin
    {
        private ITraceLogger _traceLogger;

        public static IConfiguration Config { get; set; }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            var currentDirectory = Path.GetDirectoryName(new Uri(typeof(Plugin).Assembly.CodeBase).LocalPath);

            _traceLogger = TraceLogManager.Instance.WithBaseDir(currentDirectory).GetLogger<Plugin>();

            Config = new ConfigurationBuilder().AddDefaults(currentDirectory).Build();

            var isEnabled = Config.GetValue("Enabled", true);

            if (isEnabled)
            {
                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    e.ReqnrollConfiguration.AdditionalStepAssemblies.Add("ReportPortal.ReqnrollPlugin");
                    e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IBindingInvoker>();
                };

                runtimePluginEvents.CustomizeScenarioDependencies += (sender, e) =>
                {
                    e.ObjectContainer.RegisterTypeAs<SkippedStepsHandler, ISkippedStepHandler>();
                };
            }
        }
    }
}
