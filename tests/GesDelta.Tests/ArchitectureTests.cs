using System.Reflection;

namespace GesDelta.Tests;

[TestFixture]
public class ArchitectureTests
{
    private static readonly string[] ForbiddenForUi =
    {
        "GesDelta.Storage",
        "GesDelta.Agents"
    };

    // Общий фильтр: какие из ссылок сборки входят в запрещённый список.
    private static List<string> FindForbiddenReferences(Assembly assembly, string[] forbidden)
    {
        return assembly.GetReferencedAssemblies()
            .Where(a => a.Name != null && forbidden.Contains(a.Name))
            .Select(a => a.Name!)
            .ToList();
    }

    private static IEnumerable<TestCaseData> UiAssemblies()
    {
        yield return new TestCaseData(typeof(App.AssemblyMarker).Assembly).SetName("App");
        yield return new TestCaseData(typeof(Cli.AssemblyMarker).Assembly).SetName("Cli");
    }

    [TestCaseSource(nameof(UiAssemblies))]
    public void UiProject_DoesNotReference_StorageOrAgents(Assembly assembly)
    {
        Assert.That(FindForbiddenReferences(assembly, ForbiddenForUi), Is.Empty);
    }

    [Test]
    public void Filter_FindsForbiddenReference()
    {
        // Явно используем тип из Core, чтобы ссылка на сборку точно попала в метаданные тестов.
        Assert.That(typeof(Core.AssemblyMarker).Assembly.GetName().Name, Is.EqualTo("GesDelta.Core"));

        var found = FindForbiddenReferences(typeof(ArchitectureTests).Assembly, new[] { "GesDelta.Core" });

        Assert.That(found, Is.EqualTo(new[] { "GesDelta.Core" }));
    }

    [Test]
    public void Filter_ReturnsEmpty_WhenNothingForbidden()
    {
        var found = FindForbiddenReferences(typeof(ArchitectureTests).Assembly, new[] { "GesDelta.NoSuchProject" });

        Assert.That(found, Is.Empty);
    }
    
    [Test]
    public void Core_DoesNotReference_AnyGesDeltaProject()
    {
        var references = typeof(Core.AssemblyMarker).Assembly.GetReferencedAssemblies();
        var violations = references
            .Where(a => a.Name != null && a.Name.StartsWith("GesDelta.", StringComparison.Ordinal))
            .Select(a=>a.Name);
        Assert.That(violations, Is.Empty);
    }
}