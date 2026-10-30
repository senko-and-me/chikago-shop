using ChikagoShop.Domain;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class ArchitectureTests
{
    [Test]
    public void DomainAssemblyDoesNotReferenceGodot()
    {
        string[] references = typeof(DomainAssembly).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.That(references, Has.None.StartsWith("Godot"));
    }
}
