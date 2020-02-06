﻿using System;
using ReportPortal.Client;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class StepStartedEventArgs : EventArgs
    {
        public StepStartedEventArgs(IClientService service, CreateLogItemRequest request)
        {
            Service = service;
            AddLogItemRequest = request;
        }

        public StepStartedEventArgs(IClientService service, CreateLogItemRequest request, ITestReporter testReporter)
            : this(service, request)
        {
            TestReporter = testReporter;
        }

        public StepStartedEventArgs(IClientService service, CreateLogItemRequest request, ITestReporter testReporter, FeatureContext featureContext, ScenarioContext scenarioContext, ScenarioStepContext stepContext)
            : this(service, request, testReporter)
        {
            FeatureContext = featureContext;
            ScenarioContext = scenarioContext;
            StepContext = stepContext;
        }

        public IClientService Service { get; }

        public CreateLogItemRequest AddLogItemRequest { get; }

        public ITestReporter TestReporter { get; }

        public FeatureContext FeatureContext { get; }

        public ScenarioContext ScenarioContext { get; }

        public ScenarioStepContext StepContext { get; }

        public bool Canceled { get; set;}
    }
}
