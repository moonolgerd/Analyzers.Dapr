using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace Dapr.Actors.Analyzers.Tests;

[TestClass]
public class DaprActorAnalyzerTests
{
    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Actors.IActor).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Actors.Runtime.Actor).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.DataContractAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new ActorSerializationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id.StartsWith("DAPR")).ToArray();
    }

    [TestMethod]
    public async Task ActorInterface_WithoutIActor_ShouldReportDAPR1405()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;

namespace Test
{
    public interface ITestActor
    {
        Task<string> GetDataAsync();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<string> GetDataAsync() => Task.FromResult(""test"");
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1405 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1405");

        Assert.IsNotNull(dapr1405, "Expected DAPR1405 diagnostic for interface missing IActor inheritance");
        Assert.IsTrue(dapr1405.GetMessage().Contains("ITestActor"));
    }

    [TestMethod]
    public async Task EnumWithoutEnumMember_ShouldReportDAPR1406()
    {
        var code = @"
namespace Test
{
    public enum TestEnum
    {
        Value1,
        Value2
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1406Diagnostics = diagnostics.Where(d => d.Id == "DAPR1406").ToArray();

        Assert.AreEqual(2, dapr1406Diagnostics.Length, "Expected DAPR1406 diagnostics for enum members without EnumMember attribute");
        Assert.IsTrue(dapr1406Diagnostics.Any(d => d.GetMessage().Contains("Value1")));
        Assert.IsTrue(dapr1406Diagnostics.Any(d => d.GetMessage().Contains("Value2")));
    }

    [TestMethod]
    public async Task ActorMethodWithComplexParameter_ShouldReportDAPR1409()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Threading.Tasks;

namespace Test
{
    public class ComplexType
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task ProcessDataAsync(ComplexType data);
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task ProcessDataAsync(ComplexType data) => Task.CompletedTask;
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1409 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1409");

        Assert.IsNotNull(dapr1409, "Expected DAPR1409 diagnostic for method parameter without serialization attributes");
        Assert.IsTrue(dapr1409.GetMessage().Contains("ComplexType"));
    }

    [TestMethod]
    public async Task ActorMethodWithComplexReturnType_ShouldReportDAPR1410()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Threading.Tasks;

namespace Test
{
    public class ComplexResult
    {
        public string Data { get; set; }
        public bool Success { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task<ComplexResult> GetResultAsync();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<ComplexResult> GetResultAsync() => Task.FromResult(new ComplexResult());
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1410 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1410");

        Assert.IsNotNull(dapr1410, "Expected DAPR1410 diagnostic for method return type without serialization attributes");
        Assert.IsTrue(dapr1410.GetMessage().Contains("ComplexResult"));
    }

    [TestMethod]
    public async Task ActorMethodWithCollectionOfComplexType_ShouldReportDAPR1411()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Test
{
    public class Item
    {
        public string Name { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task<List<Item>> GetItemsAsync();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<List<Item>> GetItemsAsync() => Task.FromResult(new List<Item>());
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1411 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1411");

        Assert.IsNotNull(dapr1411, "Expected DAPR1411 diagnostic for collection with complex element type");
        Assert.IsTrue(dapr1411.GetMessage().Contains("List"));
        Assert.IsTrue(dapr1411.GetMessage().Contains("Item"));
    }

    [TestMethod]
    public async Task ValidActorWithDataContract_ShouldNotReportDiagnostics()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Test
{
    [DataContract]
    public class ValidComplexType
    {
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public int Value { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task<ValidComplexType> GetDataAsync(ValidComplexType input);
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<ValidComplexType> GetDataAsync(ValidComplexType input) => Task.FromResult(input);
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var complexTypeDiagnostics = diagnostics.Where(d => d.Id == "DAPR1409" || d.Id == "DAPR1410").ToArray();

        Assert.AreEqual(0, complexTypeDiagnostics.Length, "Should not report diagnostics for types with DataContract attribute");
    }

    [TestMethod]
    public async Task ActorMethodWithPrimitiveParameters_ShouldNotReportWarning()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System;
using System.Threading.Tasks;

namespace Test
{
    public interface ITestActor : IActor
    {
        Task<string> ProcessAsync(string input, int count, DateTime timestamp);
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<string> ProcessAsync(string input, int count, DateTime timestamp) 
            => Task.FromResult($""{input}_{count}_{timestamp}"");
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var parameterDiagnostics = diagnostics.Where(d => d.Id == "DAPR1409" || d.Id == "DAPR1410").ToArray();

        Assert.AreEqual(0, parameterDiagnostics.Length, "Should not report diagnostics for primitive types");
    }

    [TestMethod]
    public async Task EnumWithEnumMemberAttributes_ShouldNotReportWarning()
    {
        var code = @"
using System.Runtime.Serialization;

namespace Test
{
    public enum Season
    {
        [EnumMember]
        Spring,
        [EnumMember]
        Summer,
        [EnumMember]
        Fall,
        [EnumMember]
        Winter
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var enumDiagnostics = diagnostics.Where(d => d.Id == "DAPR1406").ToArray();

        Assert.AreEqual(0, enumDiagnostics.Length, "Should not report diagnostics for enum members with EnumMember attribute");
    }

    [TestMethod]
    public async Task RecordWithoutDataContract_ShouldReportDAPR1412()
    {
        var code = @"
using System;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Threading.Tasks;

namespace Test
{
    public record Doodad(Guid Id, string Name, int Count);

    public interface IDoodadActor : IActor
    {
        Task<Doodad> GetAsync();
    }

    public class DoodadActor : Actor, IDoodadActor
    {
        public DoodadActor(ActorHost host) : base(host) { }
        public Task<Doodad> GetAsync() => Task.FromResult(new Doodad(Guid.NewGuid(), "", 0));
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1412 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1412");

        Assert.IsNotNull(dapr1412, "Expected DAPR1412 diagnostic for record without DataContract attribute");
        Assert.IsTrue(dapr1412.GetMessage().Contains("Doodad"));
    }

    [TestMethod]
    public async Task RecordNotUsedInActorMethod_ShouldNotReportDAPR1412()
    {
        var code = @"
using System;

namespace Test
{
    public record Standalone(Guid Id, string Name);
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1412Diagnostics = diagnostics.Where(d => d.Id == "DAPR1412").ToArray();

        Assert.AreEqual(0, dapr1412Diagnostics.Length, "Should not report DAPR1412 for records not used in public Dapr actor methods");
    }

    [TestMethod]
    public async Task RecordWithDataContractButMissingDataMember_ShouldReportDAPR1412()
    {
        var code = @"
using System;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Test
{
    [DataContract]
    public record Doodad(Guid Id, string Name, int Count);

    public interface IDoodadActor : IActor
    {
        Task<Doodad> GetAsync();
    }

    public class DoodadActor : Actor, IDoodadActor
    {
        public DoodadActor(ActorHost host) : base(host) { }
        public Task<Doodad> GetAsync() => Task.FromResult(new Doodad(Guid.NewGuid(), "", 0));
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1412Diagnostics = diagnostics.Where(d => d.Id == "DAPR1412").ToArray();

        Assert.IsTrue(dapr1412Diagnostics.Length > 0, "Expected DAPR1412 diagnostics for record parameters without DataMember attributes");
    }

    [TestMethod]
    public async Task RecordWithProperDataContractAndDataMember_ShouldNotReportDAPR1412()
    {
        var code = @"
using System;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Test
{
    [DataContract]
    public record Doodad(
        [property: DataMember] Guid Id,
        [property: DataMember] string Name,
        [property: DataMember] int Count);

    public interface IDoodadActor : IActor
    {
        Task<Doodad> GetAsync();
    }

    public class DoodadActor : Actor, IDoodadActor
    {
        public DoodadActor(ActorHost host) : base(host) { }
        public Task<Doodad> GetAsync() => Task.FromResult(new Doodad(Guid.NewGuid(), "", 0));
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1412Diagnostics = diagnostics.Where(d => d.Id == "DAPR1412").ToArray();

        Assert.AreEqual(0, dapr1412Diagnostics.Length, "Should not report diagnostics for record with proper DataContract and DataMember attributes");
    }

    [TestMethod]
    public async Task RecordUsedInActorMethod_ShouldReportDAPR1412WhenMissingAttributes()
    {
        var code = @"
using System;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Threading.Tasks;

namespace Test
{
    public record UserData(string Name, int Age, DateTime CreatedAt);

    public interface ITestActor : IActor
    {
        Task<UserData> GetUserDataAsync(UserData input);
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<UserData> GetUserDataAsync(UserData input) => Task.FromResult(input);
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1412 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1412");

        Assert.IsNotNull(dapr1412, "Expected DAPR1412 diagnostic for record used in Actor method without proper attributes");
        Assert.IsTrue(dapr1412.GetMessage().Contains("UserData"));
    }

    [TestMethod]
    public async Task ActorClass_WithoutIActorInterface_ShouldReportDAPR1413()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;

namespace Test
{
    public class TestActor : Actor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public void DoSomething() { }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1413 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1413");

        Assert.IsNotNull(dapr1413, "Expected DAPR1413 diagnostic for Actor class without IActor interface");
        Assert.IsTrue(dapr1413.GetMessage().Contains("TestActor"));
    }

    [TestMethod]
    public async Task ActorClass_WithIActorInterface_ShouldNotReportDAPR1413()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;

namespace Test
{
    public interface ITestActor : IActor
    {
        void DoSomething();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public void DoSomething() { }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1413 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1413");

        Assert.IsNull(dapr1413, "Should not report DAPR1413 for Actor class with proper IActor interface");
    }

    [TestMethod]
    public async Task TypeWithoutParameterlessConstructorOrDataContract_ShouldReportDAPR1414()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Threading.Tasks;

namespace Test
{
    public class TypeWithoutParameterlessConstructor
    {
        public TypeWithoutParameterlessConstructor(string value)
        {
            Value = value;
        }
        
        public string Value { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task<TypeWithoutParameterlessConstructor> GetDataAsync();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<TypeWithoutParameterlessConstructor> GetDataAsync()
        {
            return Task.FromResult(new TypeWithoutParameterlessConstructor(""test""));
        }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1414 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1414");

        Assert.IsNotNull(dapr1414, "Expected DAPR1414 diagnostic for type without parameterless constructor or DataContract");
        Assert.IsTrue(dapr1414.GetMessage().Contains("TypeWithoutParameterlessConstructor"));
    }

    [TestMethod]
    public async Task TypeWithDataContract_ShouldNotReportDAPR1414()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Test
{
    [DataContract]
    public class TypeWithDataContract
    {
        public TypeWithDataContract(string value)
        {
            Value = value;
        }
        
        [DataMember]
        public string Value { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task<TypeWithDataContract> GetDataAsync();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<TypeWithDataContract> GetDataAsync()
        {
            return Task.FromResult(new TypeWithDataContract(""test""));
        }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1414 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1414");

        Assert.IsNull(dapr1414, "Should not report DAPR1414 for type with DataContract attribute");
    }

    [TestMethod]
    public async Task TypeWithParameterlessConstructor_ShouldNotReportDAPR1414()
    {
        var code = @"
using Dapr.Actors;
using Dapr.Actors.Runtime;
using System.Threading.Tasks;

namespace Test
{
    public class TypeWithParameterlessConstructor
    {
        public TypeWithParameterlessConstructor()
        {
        }
        
        public TypeWithParameterlessConstructor(string value)
        {
            Value = value;
        }
        
        public string Value { get; set; }
    }

    public interface ITestActor : IActor
    {
        Task<TypeWithParameterlessConstructor> GetDataAsync();
    }

    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorHost host) : base(host) { }
        
        public Task<TypeWithParameterlessConstructor> GetDataAsync()
        {
            return Task.FromResult(new TypeWithParameterlessConstructor());
        }
    }
}";
        var diagnostics = await GetDiagnosticsAsync(code);
        var dapr1414 = diagnostics.FirstOrDefault(d => d.Id == "DAPR1414");

        Assert.IsNull(dapr1414, "Should not report DAPR1414 for type with parameterless constructor");
    }
}
