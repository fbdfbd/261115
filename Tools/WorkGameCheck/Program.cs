using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using WorkGame.Tests;

// Runs the same pure-core NUnit cases when Unity cannot start in a headless environment.
// Unity-native asset/editor tests still require the Unity Test Runner.
int passed = 0;
int failed = 0;
foreach (var fixture in new object[] { new WorkSystemTests(), new WorkPresenterTests(), new WorkDeskLayoutTests() })
foreach (var method in fixture.GetType().GetMethods())
{
    var cases = method.GetCustomAttributes<TestCaseAttribute>().ToArray();
    if (cases.Length == 0 && method.GetCustomAttribute<TestAttribute>() == null) continue;
    var arguments = cases.Length == 0 ? new[] { Array.Empty<object>() } : cases.Select(x => x.Arguments).ToArray();
    foreach (var values in arguments)
    {
        string name = method.Name + "(" + string.Join(", ", values) + ")";
        try
        {
            method.Invoke(fixture, values);
            Console.WriteLine("PASS " + name);
            passed++;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL " + name + "\n" + (e.InnerException ?? e));
            failed++;
        }
    }
}
Console.WriteLine($"{passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;
