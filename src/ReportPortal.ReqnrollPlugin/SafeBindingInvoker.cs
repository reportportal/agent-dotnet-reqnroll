using Reqnroll;
using Reqnroll.Bindings;
using Reqnroll.BoDi;
using Reqnroll.Configuration;
using Reqnroll.ErrorHandling;
using Reqnroll.Infrastructure;
using Reqnroll.Tracing;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ReportPortal.ReqnrollPlugin
{
    internal class SafeBindingInvoker : BindingInvoker
    {
        public SafeBindingInvoker(ReqnrollConfiguration reqnrollConfiguration, IErrorProvider errorProvider, IBindingDelegateInvoker bindingDelegateInvoker, IObjectContainer objectContainer)
            : base(reqnrollConfiguration, errorProvider, bindingDelegateInvoker, objectContainer)
        {
        }

        public override async Task<object> InvokeBindingAsync(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, DurationHolder durationHolder)
        {
            object result = null;

            try
            {
                result = await base.InvokeBindingAsync(binding, contextManager, arguments, testTracer, durationHolder);
            }
            catch (Exception ex)
            {
                PreserveStackTrace(ex);

                if (binding is IHookBinding == false)
                {
                    throw;
                }

                var hookBinding = binding as IHookBinding;

                if (hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeScenarioBlock
                    || hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeStep
                    || hookBinding.HookType == HookType.AfterStep
                    || hookBinding.HookType == HookType.AfterScenario
                    || hookBinding.HookType == HookType.AfterScenarioBlock)
                {
                    SetTestError(contextManager.ScenarioContext, ex);
                }
            }

            return result;
        }

        private static void SetTestError(ScenarioContext context, Exception ex)
        {
            if (context != null && context.TestError == null)
            {
                context.GetType().GetProperty("ScenarioExecutionStatus")
                    ?.SetValue(context, ScenarioExecutionStatus.TestError);

                context.GetType().GetProperty("TestError")
                    ?.SetValue(context, ex);
            }
        }

        private static void PreserveStackTrace(Exception ex)
        {
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(ex, new object[0]);
        }
    }
}
