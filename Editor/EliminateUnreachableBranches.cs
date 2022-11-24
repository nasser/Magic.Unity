using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace Magic.Unity
{
    public static class EliminateUnreachableBranches
    {
        public static void RewriteMethod(MethodDefinition m)
        {
            var branchTargets = new HashSet<Instruction>();
            var potentiallyUnreachableBranches = new HashSet<Instruction>();
            var removedBranches = false;

            var lastInstruction = m.Body.Instructions[0];
            foreach (var i in m.Body.Instructions)
            {
                if (i.OpCode == OpCodes.Br
                   || i.OpCode == OpCodes.Brfalse
                   || i.OpCode == OpCodes.Brtrue)
                    branchTargets.Add((Instruction)i.Operand);

                if (i.OpCode == OpCodes.Br && (lastInstruction.OpCode == OpCodes.Br || lastInstruction.OpCode == OpCodes.Throw))
                    potentiallyUnreachableBranches.Add(i);
                lastInstruction = i;
            }

            foreach (var i in potentiallyUnreachableBranches)
            {
                if (!branchTargets.Contains(i))
                {
                    Debug.LogFormat("[Magic.Unity/EliminateUnreachableBranches] removing unreachable branch from {1} ({0})", i, m);
                    m.Body.Instructions.Remove(i);
                    removedBranches = true;
                }
            }

            if (removedBranches)
                RewriteMethod(m);
        }
    }
}