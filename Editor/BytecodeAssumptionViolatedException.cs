using System;

namespace Magic.Unity
{
    public class BytecodeAssumptionViolatedException : Exception
    {
        public BytecodeAssumptionViolatedException(string message) : base(message) { }
    }
}