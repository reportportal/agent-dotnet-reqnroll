using ReportPortal.ReqnrollPlugin.Extensions;
using Reqnroll;
using Reqnroll.Infrastructure;
using System;

namespace ReportPortal.ReqnrollPlugin
{
    public class SkippedStepsHandler : ISkippedStepHandler
    {
        public void Handle(ScenarioContext scenarioContext)
        {
            var scenarioReporter = ReportPortalAddin.GetScenarioTestReporter(scenarioContext);

            var skippedStepReporter = scenarioReporter.StartChildTestReporter(new Client.Abstractions.Requests.StartTestItemRequest
            {
                Name = scenarioContext.StepContext.StepInfo.GetCaption(),
                StartTime = DateTime.UtcNow,
                Type = Client.Abstractions.Models.TestItemType.Step,
                HasStats = false
            });
            
            skippedStepReporter.Finish(new Client.Abstractions.Requests.FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = Client.Abstractions.Models.Status.Skipped
            });
        }
    }
}
