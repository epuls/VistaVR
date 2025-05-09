﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Unity.Entities.SourceGen.SystemGenerator.SystemAPI;
using VerifyCS =
    Unity.Entities.SourceGenerators.Test.CSharpSourceGeneratorVerifier<
        Unity.Entities.SourceGen.SystemGenerator.SystemGenerator>;

namespace Unity.Entities.SourceGenerators;

[TestClass]
public class SystemAPIErrorTests
{
    [TestMethod]
    public async Task NO_SGICE_BUT_HAS_CSHARP_COMPILE_ERRORS()
    {
        const string source = @"
            using Unity.Entities;
            using Unity.Entities.Tests;
            using static Unity.Entities.SystemAPI;
            partial struct SomeJobEntity : IJobEntity
            {
                public void Execute(in EcsTestData id){}
            }

            partial struct SomeSystem : ISystem {
                public void OnUpdate(ref SystemState state) {
                    var hadComp = {|#0:HasComponent<EcsTestData>|}();
                    new SomeJobEntity().ScheduleParallel({|#1:SystemAPI.Query<RefRO<EcsTestData>>().WithEntityAccess()|});
                }
            }";
        var expectedA = VerifyCS.CompilerError("CS7036").WithLocation(0);
        var expectedB = VerifyCS.CompilerError("CS1503").WithLocation(1);
        await VerifyCS.VerifySourceGeneratorAsync(source, expectedA, expectedB);
    }

    [TestMethod]
    public async Task SGSA0001()
    {
        const string source = @"
            using Unity.Entities;
            using Unity.Entities.Tests;
            using static Unity.Entities.SystemAPI;

            partial struct SomeSystem : ISystem {
                public void OnUpdate(ref SystemState state) {
                    Idk<EcsTestData>();
                }

                public void Idk<T>() where T:struct,IComponentData{
                    var hadComp = {|#0:HasComponent<T>(default)|};
                }
            }";
        var expected = VerifyCS.CompilerError(nameof(SystemApiContextErrors.SGSA0001)).WithLocation(0);
        await VerifyCS.VerifySourceGeneratorAsync(source, expected);
    }

    [TestMethod]
    public async Task SGSA0002()
    {
        const string source = @"
            using Unity.Entities;
            using Unity.Entities.Tests;
            using static Unity.Entities.SystemAPI;

            partial struct SomeSystem : ISystem {
                public void OnUpdate(ref SystemState state) {
                    var ro = false;
                    var lookup = {|#0:GetComponentLookup<EcsTestData>(ro)|};
                }
            }";
        var expected = VerifyCS.CompilerError(nameof(SystemApiContextErrors.SGSA0002)).WithLocation(0);
        await VerifyCS.VerifySourceGeneratorAsync(source, expected);
    }
}
