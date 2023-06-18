using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnityEngine;

namespace Magic.Unity
{
    public static class EliminateUnreachableInstructions
    {
        public static void RewriteMethod(MethodDefinition m)
        {
            if (m.HasBody)
            {
                var reachable = new HashSet<Instruction>();
                Mark(m, reachable);
                Sweep(m, reachable);
            }
        }

        static void Mark(MethodDefinition method, HashSet<Instruction> reachable)
        {
            var instructions = method.Body.Instructions;
            var remaining = new Queue<Instruction>(instructions.Count);

            // methods enter at first instruction
            remaining.Enqueue(instructions.First());

            while (remaining.Count > 0)
            {
                var instruction = remaining.Dequeue();

                // skip instruction if already visited
                if (reachable.Contains(instruction))
                    continue;

                // mark instruction as reachable
                reachable.Add(instruction);

                // if instruction is the start of a try/catch/finally region enqueue the
                // region's catch and finally handlers
                foreach (var eh in method.Body.ExceptionHandlers)
                    if (instruction == eh.TryStart)
                        remaining.Enqueue(eh.HandlerStart);

                switch (instruction.OpCode.FlowControl)
                {
                    // most instructions just reach the next instruction
                    case FlowControl.Call:
                    case FlowControl.Meta:
                    case FlowControl.Break:
                    case FlowControl.Next:
                        remaining.Enqueue(instruction.Next);
                        break;
                    // conditional branches reach next instruction and branch target(s)
                    case FlowControl.Cond_Branch:
                        remaining.Enqueue(instruction.Next);
                        goto case FlowControl.Branch;
                    // branches only reach their branch target(s)
                    case FlowControl.Branch:
                        if (instruction.Operand is Instruction branchTarget)
                            remaining.Enqueue(branchTarget);
                        else if (instruction.Operand is Instruction[] branchTargets)
                            foreach (var target in branchTargets)
                                remaining.Enqueue(target);
                        else
                            throw new BytecodeAssumptionViolatedException($"Branch instruction operand not instruction {instruction}");
                        break;
                    // throw and return reach no instructions
                    case FlowControl.Throw:
                    case FlowControl.Return:
                        break;
                    // FlowControl.Phi is not covered but should be extinct in the wild
                    default:
                        throw new NotImplementedException($"Unexpected control flow {instruction.OpCode.FlowControl} in {instruction}");
                }
            }
        }

        static void Sweep(MethodDefinition method, HashSet<Instruction> reachable)
        {
            // no work to do if all instructions are reachable
            if (method.Body.Instructions.Count == reachable.Count)
                return;

            UnityEngine.Debug.Log($"[EliminateUnreachableInstructions] {method}");
            var unreachable = new HashSet<Instruction>();

            // gather all unreachable instructions from method
            foreach (var instruction in method.Body.Instructions)
                if (!reachable.Contains(instruction))
                    unreachable.Add(instruction);
            
            // remove all unreachable instructions
            foreach(var instruction in unreachable)
            {
                UnityEngine.Debug.Log($"[EliminateUnreachableInstructions] {instruction}");
                method.Body.Instructions.Remove(instruction);
            }
        }
    }
}