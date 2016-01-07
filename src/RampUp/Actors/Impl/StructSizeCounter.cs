using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RampUp.Actors.Impl
{
    public class StructSizeCounter : IStructSizeCounter
    {
        private const int ProbeSize = 10 * 1024;
        private readonly Dictionary<Type, int> _sizes = new Dictionary<Type, int>();

        public int GetSize(Type @struct)
        {
            int size;
            if (_sizes.TryGetValue(@struct, out size))
            {
                return size;
            }

            Assert(@struct);

            var dm = new DynamicMethod("GetSizeOf" + @struct.Namespace.Replace(".", "_") + @struct.Name, typeof(int), Type.EmptyTypes,
                @struct.Assembly.Modules.First(), true);
            var il = dm.GetILGenerator();
            il.DeclareLocal(typeof(byte*));

            il.Emit(OpCodes.Ldc_I4, ProbeSize);
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Localloc);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            var initBytes = typeof(StructSizeCounter).GetMethod("InitBytes", BindingFlags.Static | BindingFlags.NonPublic);
            il.EmitCall(OpCodes.Call, initBytes, null);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Initobj, @struct);
            il.Emit(OpCodes.Ldloc_0);
            var countZeroes = typeof(StructSizeCounter).GetMethod("CountZeros", BindingFlags.Static | BindingFlags.NonPublic);
            il.EmitCall(OpCodes.Call, countZeroes, null);
            il.Emit(OpCodes.Ret);

            size = ((Func<int>)dm.CreateDelegate(typeof(Func<int>)))();
            _sizes[@struct] = size;
            return size;
        }

        private void Assert(Type t)
        {
            if (t.IsValueType == false && t.IsPointer == false)
            {
                throw new ArgumentException($"This counts size only for value types. {t.FullName} is not a value type.");
            }

            var fieldTypes = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(fi => fi.FieldType)
                .Distinct();

            foreach (var ft in fieldTypes)
            {
                if (ft.IsValueType || ft.IsPointer)
                {
                    if (ft.IsPrimitive == false)
                    {
                        GetSize(ft);
                    }
                }
                else
                {
                    throw new ArgumentException($"This counts size only for value types. {t.FullName} is not a value type.");
                }
            }
        }

        private static unsafe int CountZeros(byte* bytes)
        {
            var size = 0;
            for (var i = 0; i < ProbeSize; i++)
            {
                size += 1 - bytes[i];
            }
            return size;
        }

        private static unsafe void InitBytes(byte* bytes)
        {
            for (var i = 0; i < ProbeSize; i++)
            {
                bytes[i] = 1;
            }
        }

        /// <summary>
        /// Just for picking up IL in disassembler.
        /// </summary>
        private struct A
        {
            public int Value;
            public Guid OtherValue;
        }

        /// <summary>
        /// Just for picking up IL in disassembler.
        /// </summary>
        /// <returns></returns>
        private static unsafe int GetManagedSize()
        {
            byte* bytes = stackalloc byte[ProbeSize];

            // first copy 1s to bytes
            InitBytes(bytes);

            *(A*)bytes = default(A);

            // count zeroes as .NET structs are zero initialized
            return CountZeros(bytes);
        }
    }
}