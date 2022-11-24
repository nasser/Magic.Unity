using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;

namespace Magic.Unity
{
    public class BytecodeAssumptionViolatedException : Exception
    {
        public BytecodeAssumptionViolatedException(string message) : base(message) { }
    }

    public static class GenerateGenericWorkaroundMethods
    {
        struct DynamicCallSiteInfo
        {
            public string name;
            public int arity;
            public bool isStatic;
        }

        static HashSet<DynamicCallSiteInfo> DynamicCallSites = new HashSet<DynamicCallSiteInfo>();
        static List<MethodDefinition> AllMethods = new List<MethodDefinition>();
        static TypeDefinition MagicRuntimeDelegateHelpers = null;

        public static void Init()
        {
            AllMethods = AppDomain
                            .CurrentDomain
                            .GetAssemblies()
                            .Select(a => AssemblyDefinition.ReadAssembly(a.Location).MainModule)
                            .SelectMany(m => m.Types)
                            .Where(t => t.IsPublic)
                            .SelectMany(t => t.Methods)
                            .Where(m => m.IsPublic)
                            .ToList();

            MagicRuntimeDelegateHelpers = AssemblyDefinition
                                            .ReadAssembly(typeof(Magic.Runtime).Assembly.Location)
                                            .MainModule
                                            .Types
                                            .Where(t => t.FullName == "Magic.DelegateHelpers").Single();
        }

        public static void StartRewriteAssembly(AssemblyDefinition assy)
        {
            DynamicCallSites.Clear();
        }

        public static void FinishRewriteAssembly(AssemblyDefinition assy)
        {
            if (DynamicCallSites.Count > 0)
            {
                MaybeEmitWorkaroundStaticType(assy, DynamicCallSites);
            }
        }

        public static void AnalyzeMethod(MethodDefinition m)
        {
            foreach (var i in m.Body.Instructions)
            {
                if (i.OpCode == OpCodes.Stsfld)
                {
                    var field = i.Operand as FieldReference;
                    if (field.FieldType.FullName.StartsWith("Magic.CallSiteZeroArityMember"))
                    {
                        var ldstrInstruction = i.Previous.Previous;
                        if (ldstrInstruction.OpCode != OpCodes.Ldstr)
                        {
                            throw new BytecodeAssumptionViolatedException($"Could not find name of callsite target member, MAGIC's bytecode patterns may have changed. (expected ldstr at {ldstrInstruction.Offset}, got {ldstrInstruction})");
                        }
                        var name = ldstrInstruction.Operand as string;
                        DynamicCallSites.Add(new DynamicCallSiteInfo
                        {
                            name = name,
                            arity = 0,
                            isStatic = false
                        });
                        DynamicCallSites.Add(new DynamicCallSiteInfo
                        {
                            name = $"get_{name}",
                            arity = 0,
                            isStatic = false
                        });
                    }
                    else if (field.FieldType.FullName.StartsWith("Magic.CallsiteInstanceMethod"))
                    {
                        var ldstrInstruction = i.Previous.Previous;
                        if (ldstrInstruction.OpCode != OpCodes.Ldstr)
                        {
                            throw new BytecodeAssumptionViolatedException($"Could not find name of callsite target member, MAGIC's bytecode patterns may have changed. (expected ldstr at {ldstrInstruction.Offset}, got {ldstrInstruction})");
                        }
                        var name = ldstrInstruction.Operand as string;
                        var invokeMethod = field.FieldType.Resolve().Methods.Where(_m => _m.Name == "Invoke").Single();
                        var arity = invokeMethod.Parameters.Count - 1;
                        DynamicCallSites.Add(new DynamicCallSiteInfo
                        {
                            name = name,
                            arity = arity,
                            isStatic = false
                        });
                    }
                    else if (field.FieldType.FullName.StartsWith("Magic.CallsiteStaticMethod"))
                    {
                        var ldstrInstruction = i.Previous.Previous;
                        if (ldstrInstruction.OpCode != OpCodes.Ldstr)
                        {
                            throw new BytecodeAssumptionViolatedException($"Could not find name of callsite target member, MAGIC's bytecode patterns may have changed. (expected ldstr at {ldstrInstruction.Offset}, got {ldstrInstruction})");
                        }
                        var name = ldstrInstruction.Operand as string;
                        var invokeMethod = field.FieldType.Resolve().Methods.Where(_m => _m.Name == "Invoke").Single();
                        var arity = invokeMethod.Parameters.Count;
                        DynamicCallSites.Add(new DynamicCallSiteInfo
                        {
                            name = name,
                            arity = arity,
                            isStatic = true
                        });
                    }
                }
            }
        }

        static void MaybeEmitWorkaroundStaticType(AssemblyDefinition assy, IEnumerable<DynamicCallSiteInfo> callSiteInfos)
        {
            var type = new TypeDefinition("Magic.Unity", "<il2cpp-workaround>", TypeAttributes.Public, assy.MainModule.TypeSystem.Object);
            var method = new MethodDefinition("<problematic-generics>", MethodAttributes.Public | MethodAttributes.Static, assy.MainModule.TypeSystem.Void);
            var invocations = 0;
            // have to add type to assembly so that module.Import in
            // EmitWorkaroundInvocation works. we remove if no invocations are
            // needed
            assy.MainModule.Types.Add(type);
            type.Methods.Add(method);
            foreach (var callSiteInfo in callSiteInfos)
            {
                var signatures = GetPrecompilationSignatures(callSiteInfo);
                foreach (var signature in signatures)
                {
                    invocations += 1;
                    EmitWorkaroundInvocation(method.Body, signature);
                }
            }
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if(invocations == 0)
            {
                // no actual invocations generated, remove type from assembly
                assy.MainModule.Types.Remove(type);
            }
        }

        public static MethodDefinition GetRuntimeDelegateHelperMethod(string name)
        {
            foreach (var method in MagicRuntimeDelegateHelpers.Methods)
            {
                if (method.Name == name)
                    return method;
            }

            throw new KeyNotFoundException($"Could not find method {name} in MAGIC runtime delegate helpers");
        }

        private static void EmitWorkaroundInvocation(MethodBody body, TypeReference[] signature)
        {
            var module = body.Method.Module;
            var arity = signature.Length - 1;
            var name = $"GetMethodDelegateFast{arity:D2}";
            var openGenericMethod = module.ImportReference(GetRuntimeDelegateHelperMethod(name));
            var closedGenericMethod = new Mono.Cecil.GenericInstanceMethod(openGenericMethod);
            foreach (var t in signature)
            {
                closedGenericMethod.GenericArguments.Add(module.ImportReference(t));
            }
            body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            body.Instructions.Add(Instruction.Create(OpCodes.Call, closedGenericMethod));
            body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }

        static TypeReference[] GetPrecompilationSignature(MethodDefinition m)
        {
            var paramTypes = m.Parameters.Select(_m => _m.ParameterType);
            var returnType = m.ReturnType == m.Module.TypeSystem.Void ? m.Module.TypeSystem.Object : m.ReturnType;
            var declaringType = m.DeclaringType;
            return m.IsStatic ? paramTypes.Append(returnType).ToArray()
                              : paramTypes.Prepend(declaringType).Append(returnType).ToArray();
        }

        static List<TypeReference[]> GetPrecompilationSignatures(DynamicCallSiteInfo callSiteInfo)
        {
            return AllMethods.Where(m => m.Name == callSiteInfo.name
                                         && m.Parameters.Count == callSiteInfo.arity
                                         && m.IsStatic == callSiteInfo.isStatic
                                         && IsPrecompilationCandidate(m))
                             .Select(GetPrecompilationSignature)
                             .ToList();
        }

        static bool IsPrecompilationCandidate(MethodDefinition m)
        {
            return m.IsPublic
                    && !m.DeclaringType.HasGenericParameters
                    && !m.HasGenericParameters
                    && !m.IsGenericInstance
                    && !m.ReturnType.HasGenericParameters
                    && !(m.ReturnType == m.Module.TypeSystem.Void)
                    && !m.ReturnType.IsGenericInstance
                    && !m.ReturnType.HasGenericParameters
                    && !m.Parameters.Any(p => p.ParameterType.IsByReference)
                    && !m.Parameters.Any(p => p.ParameterType.HasGenericParameters)
                    && !m.Parameters.Any(p => p.ParameterType.IsGenericInstance)
                    && (m.Parameters.Any(p => p.ParameterType.IsValueType)
                        || m.ReturnType.IsValueType
                        || (m.DeclaringType.IsValueType
                            && !m.DeclaringType.IsByReference
                            && !m.IsStatic));
        }
    }
}