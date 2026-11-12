using System;
using System.IO;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

// QA-only: copy into the test assembly of an isolated project, never into production Assets.
[assembly: TestRunCallback(typeof(SecurityWindowsQaResult))]
public sealed class SecurityWindowsQaResult : ITestRunCallback
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Loaded() => Debug.Log("SECURITY_WINDOWS_QA_CALLBACK_LOADED");
    public void RunStarted(ITest testsToRun) => Debug.Log("SECURITY_WINDOWS_RUN_STARTED");
    public void TestStarted(ITest test) => Debug.Log("SECURITY_WINDOWS_TEST_STARTED " + test.FullName);
    public void TestFinished(ITestResult result)
    {
        if (!result.Test.IsSuite)
            Debug.Log("SECURITY_WINDOWS_TEST " + result.ResultState.Status + " " + result.Test.FullName + " " + result.Message);
    }
    public void RunFinished(ITestResult result)
    {
        string path = Environment.GetEnvironmentVariable("OH_LOCAL_RESULTS");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, result.ToXml(true).OuterXml);
        Application.Quit(result.FailCount == 0 ? 0 : 1);
    }
}
