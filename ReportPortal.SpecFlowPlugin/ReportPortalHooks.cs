﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using ReportPortal.SpecFlowPlugin.Configuration;
using ReportPortal.SpecFlowPlugin.EventArguments;
using ReportPortal.SpecFlowPlugin.Extensions;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    [Binding]
    internal class ReportPortalHooks : Steps
    {
        [BeforeTestRun(Order = -20000)]
        public static void BeforeTestRun()
        {
            var config = Initialize();

            if (config.IsEnabled)
            {
                var request = new StartLaunchRequest
                {
                    Name = config.Launch.Name,
                    StartTime = DateTime.UtcNow
                };

                if (config.Launch.IsDebugMode)
                {
                    request.Mode = LaunchMode.Debug;
                }

                request.Tags = config.Launch.Tags;

                var eventArg = new RunStartedEventArgs(Bridge.Service, request);
                ReportPortalAddin.OnBeforeRunStarted(null, eventArg);

                if (eventArg.LaunchReporter != null)
                {
                    Bridge.Context.LaunchReporter = eventArg.LaunchReporter;
                }

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter = Bridge.Context.LaunchReporter ?? new LaunchReporter(Bridge.Service);

                    Bridge.Context.LaunchReporter.Start(request);

                    ReportPortalAddin.OnAfterRunStarted(null, new RunStartedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                }
            }
        }

        private static Config Initialize()
        {
            var args = new InitializingEventArgs(Plugin.Config);

            ReportPortalAddin.OnInitializing(null, args);

            if (args.Config.IsEnabled)
            {
                var uri = args.Config.Server.Url;
                var project = args.Config.Server.Project;
                var uuid = args.Config.Server.Authentication.Uuid;

                if (args.Service != null)
                {
                    Bridge.Service = args.Service;
                }
                else if (args.Config.Server.Proxy != null)
                {
                    var proxy = new WebProxy(args.Config.Server.Proxy);

                    Bridge.Service = new Service(uri, project, uuid, proxy);
                }
                else
                {
                    Bridge.Service = new Service(uri, project, uuid);
                }
            }

            return args.Config;
        }

        [AfterTestRun(Order = 20000)]
        public static void AfterTestRun()
        {
            if (Bridge.Context.LaunchReporter != null)
            {
                var request = new FinishLaunchRequest
                {
                    EndTime = DateTime.UtcNow
                };

                var eventArg = new RunFinishedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter);
                ReportPortalAddin.OnBeforeRunFinished(null, eventArg);

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter.Finish(request);

                    var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReportPortal.log");
                    try
                    {
                        var sw = Stopwatch.StartNew();

                        File.AppendAllText(logFile, $"Finishing to send results to ReportPortal...{Environment.NewLine}");
                        Bridge.Context.LaunchReporter.FinishTask.Wait();
                        File.AppendAllText(logFile, $"Elapsed: {sw.Elapsed}{Environment.NewLine}");
                    }
                    catch (Exception exp)
                    {
                        File.AppendAllText(logFile, $"{exp}{Environment.NewLine}");
                    }

                    ReportPortalAddin.OnAfterRunFinished(null, new RunFinishedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                }
            }
        }

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            if (Bridge.Context.LaunchReporter != null)
            {
                lock (LockHelper.GetLock(FeatureInfoEqualityComparer.GetFeatureInfoHashCode(featureContext.FeatureInfo)))
                {
                    var currentFeature = ReportPortalAddin.GetFeatureTestReporter(featureContext);

                    if (currentFeature == null || currentFeature.FinishTask != null)
                    {
                        var request = new StartTestItemRequest
                        {
                            Name = featureContext.FeatureInfo.Title,
                            Description = featureContext.FeatureInfo.Description,
                            StartTime = DateTime.UtcNow,
                            Type = TestItemType.Suite,
                            Tags = new List<string>(featureContext.FeatureInfo.Tags)
                        };

                        var eventArg = new TestItemStartedEventArgs(Bridge.Service, request, null, featureContext, null);
                        ReportPortalAddin.OnBeforeFeatureStarted(null, eventArg);

                        if (!eventArg.Canceled)
                        {
                            currentFeature = Bridge.Context.LaunchReporter.StartNewTestNode(request);
                            ReportPortalAddin.SetFeatureTestReporter(featureContext, currentFeature);

                            ReportPortalAddin.OnAfterFeatureStarted(null, new TestItemStartedEventArgs(Bridge.Service, request, currentFeature, featureContext, null));
                        }
                    }
                    else
                    {
                        ReportPortalAddin.IncrementFeatureThreadCount(featureContext);
                    }
                }
            }
        }

        [AfterFeature(Order = 20000)]
        public static void AfterFeature(FeatureContext featureContext)
        {
            lock (LockHelper.GetLock(FeatureInfoEqualityComparer.GetFeatureInfoHashCode(featureContext.FeatureInfo)))
            {
                var currentFeature = ReportPortalAddin.GetFeatureTestReporter(featureContext);
                var remainingThreadCount = ReportPortalAddin.DecrementFeatureThreadCount(featureContext);

                if (currentFeature != null && currentFeature.FinishTask == null && remainingThreadCount == 0)
                {
                    var request = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = Status.Skipped
                    };

                    var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, currentFeature, featureContext, null);
                    ReportPortalAddin.OnBeforeFeatureFinished(null, eventArg);

                    if (!eventArg.Canceled)
                    {
                        currentFeature.Finish(request);

                        ReportPortalAddin.OnAfterFeatureFinished(null, new TestItemFinishedEventArgs(Bridge.Service, request, currentFeature, featureContext, null));
                    }
                }
            }
        }

        [BeforeScenario(Order = -20000)]
        public void BeforeScenario()
        {
            var currentFeature = ReportPortalAddin.GetFeatureTestReporter(this.FeatureContext);

            if (currentFeature != null)
            {
                var request = new StartTestItemRequest
                {
                    Name = this.ScenarioContext.ScenarioInfo.Title,
                    Description = this.ScenarioContext.ScenarioInfo.Description,
                    StartTime = DateTime.UtcNow,
                    Type = TestItemType.Step,
                    Tags = new List<string>(this.ScenarioContext.ScenarioInfo.Tags)
                };

                var eventArg = new TestItemStartedEventArgs(Bridge.Service, request, currentFeature, this.FeatureContext, this.ScenarioContext);
                ReportPortalAddin.OnBeforeScenarioStarted(this, eventArg);

                if (!eventArg.Canceled)
                {
                    var currentScenario = currentFeature.StartNewTestNode(request);
                    ReportPortalAddin.SetScenarioTestReporter(this.ScenarioContext, currentScenario);

                    ReportPortalAddin.OnAfterScenarioStarted(this, new TestItemStartedEventArgs(Bridge.Service, request, currentFeature, this.FeatureContext, this.ScenarioContext));
                }
            }
        }

        [AfterScenario(Order = 20000)]
        public void AfterScenario()
        {
            var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

            if (currentScenario != null)
            {
                Issue issue = null;
                var status = Status.Passed;

                switch (this.ScenarioContext.ScenarioExecutionStatus)
                {
                    case ScenarioExecutionStatus.TestError:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.ToInvestigate,
                            Comment = this.ScenarioContext.TestError?.Message
                        };

                        break;
                    case ScenarioExecutionStatus.BindingError:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.AutomationBug,
                            Comment = this.ScenarioContext.TestError?.Message
                        };

                        break;
                    case ScenarioExecutionStatus.UndefinedStep:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.AutomationBug,
                            Comment = new MissingStepDefinitionException().Message
                        };

                        break;
                    case ScenarioExecutionStatus.StepDefinitionPending:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.ToInvestigate,
                            Comment = "Pending"
                        };

                        break;
                }

                var request = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow.AddMilliseconds(1),
                    Status = status,
                    Issue = issue
                };

                var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, currentScenario, this.FeatureContext, this.ScenarioContext);
                ReportPortalAddin.OnBeforeScenarioFinished(this, eventArg);

                if (!eventArg.Canceled)
                {
                    if(this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                    {
                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.ToString()
                        });
                    } else if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.BindingError)
                    {
                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.Message
                        });
                    } else if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep)
                    {
                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = new MissingStepDefinitionException().Message
                        });
                    }

                    currentScenario.Finish(request);

                    ReportPortalAddin.OnAfterScenarioFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, currentScenario, this.FeatureContext, this.ScenarioContext));
                }
            }
        }

        [BeforeStep(Order = -20000)]
        public void BeforeStep()
        {
            var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

            if (currentScenario != null)
            {
                var stepInfoRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Info,
                    Time = DateTime.UtcNow,
                    Text = this.StepContext.StepInfo.GetFullText()
                };

                var eventArg = new StepStartedEventArgs(Bridge.Service, stepInfoRequest, currentScenario, this.FeatureContext, this.ScenarioContext, this.StepContext);
                ReportPortalAddin.OnBeforeStepStarted(this, eventArg);

                if (!eventArg.Canceled)
                {
                    currentScenario.Log(stepInfoRequest);
                    ReportPortalAddin.OnAfterStepStarted(this, eventArg);
                }
            }
        }
        
        [AfterStep(Order = 20000)]
        public void AfterStep()
        {
            var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

            if (currentScenario != null)
            {
                var eventArg = new StepFinishedEventArgs(Bridge.Service, null, currentScenario, this.FeatureContext, this.ScenarioContext, this.StepContext);
                ReportPortalAddin.OnBeforeStepFinished(this, eventArg);

                if (!eventArg.Canceled)
                {
                    ReportPortalAddin.OnAfterStepFinished(this, eventArg);
                }
            }
        }
    }
}
