using System;
using System.Runtime.CompilerServices;

namespace IcppReading
{
    public class NodeBase
    {
        public int Value;
        public NodeBase(int value) { Value = value; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int Evaluate(int delta) { return Value + delta; }
    }
    public sealed class Counter : NodeBase
    {
        public Counter(int value) : base(value) { }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Step(int delta) { Value += delta; return Value; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int Evaluate(int delta) { return Value * 2 + delta; }
    }
    public static class ReadingProbe
    {
        public static NodeBase Held;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Add(int a, int b) { return a + b; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int CallVirtual(NodeBase target, int delta) { return target.Evaluate(delta); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Echo<T>(T value) { return value; }
        public static string Run()
        {
            var counter = new Counter(7);
            int sum = Add(2, 3);
            int field = counter.Step(4);
            int dispatched = CallVirtual(counter, 3);
            int generic = Echo<int>(42);
            string text = Echo<string>("same");
            Held = counter;
            if (!object.ReferenceEquals(Held, counter) || Held.Value != 11)
                throw new InvalidOperationException("hold/field identity");
            Held = null;
            return sum + "," + field + "," + dispatched + "," + generic + "," + text;
        }
    }
}
