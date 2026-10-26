using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using IcppReading;

internal static class Program
{
    static readonly Dictionary<ushort, OpCode> Codes = new Dictionary<ushort, OpCode>();
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static int Main()
    {
        Require(ReadingProbe.Run() == "5,11,25,42,same", "source result mismatch");
        Require(ReadingProbe.Held == null, "hold/clear not completed");
        Require(typeof(Counter).BaseType == typeof(NodeBase), "inheritance");
        Require(typeof(Counter).GetMethod("Evaluate").GetBaseDefinition().DeclaringType == typeof(NodeBase), "override identity");
        Console.WriteLine("DESKTOP_SOURCE_CHECK passed: 5,11,25,42,same; Held=null");
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.GetValue(null) is OpCode code) Codes[unchecked((ushort)code.Value)] = code;
        foreach (Type type in new[] { typeof(NodeBase), typeof(Counter), typeof(ReadingProbe) })
        {
            foreach (ConstructorInfo method in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) Dump(method);
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) Dump(method);
        }
        return 0;
    }
    static void Dump(MethodBase method)
    {
        byte[] il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null) return;
        Console.WriteLine("METHOD " + method.DeclaringType.FullName + "::" + method.Name + " TOKEN=0x" + method.MetadataToken.ToString("X8"));
        int at = 0;
        while (at < il.Length)
        {
            int offset = at;
            ushort value = il[at++];
            if (value == 0xfe) value = (ushort)(0xfe00 | il[at++]);
            OpCode op = Codes[value];
            int width;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: width = 0; break;
                case OperandType.ShortInlineI: case OperandType.ShortInlineVar: case OperandType.ShortInlineBrTarget: width = 1; break;
                case OperandType.InlineVar: width = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: width = 8; break;
                case OperandType.InlineSwitch: width = 4 + BitConverter.ToInt32(il, at) * 4; break;
                default: width = 4; break;
            }
            Require(at + width <= il.Length, "truncated IL");
            string operand = width == 0 ? "" : BitConverter.ToString(il, at, width);
            if (op.OperandType == OperandType.InlineMethod || op.OperandType == OperandType.InlineField || op.OperandType == OperandType.InlineType || op.OperandType == OperandType.InlineTok)
            {
                int token = BitConverter.ToInt32(il, at);
                try
                {
                    MemberInfo target = method.Module.ResolveMember(token,
                        method.DeclaringType.GetGenericArguments(), method is MethodInfo m ? m.GetGenericArguments() : Type.EmptyTypes);
                    operand = "0x" + token.ToString("X8") + " " + target.DeclaringType + "::" + target;
                }
                catch (ArgumentException) { operand = "0x" + token.ToString("X8") + " unresolved"; }
            }
            Console.WriteLine((" IL_" + offset.ToString("X4") + ": " + op.Name + " " + operand).TrimEnd());
            at += width;
        }
    }
}
