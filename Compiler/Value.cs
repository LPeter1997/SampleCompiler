using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Compiler
{
    /// <summary>
    /// Mivel egy dinamikus típusú nyelvet készítünk, az értékek is többalakúak kellenek 
    /// hogy legyenek.
    /// </summary>
    public abstract class Value
    {
        private bool IsType<T>() where T : Value { return (this as T) != null; }
        private T AsType<T>() where T : Value
        {
            var value = this as T;
            if (value == null)
            {
                // TODO: Janky error
                throw new TypeError { Expected = typeof(T).Name, Got = GetType().Name };
            }
            return value;
        }

        // Típus lekérdezések

        public bool IsVoid() => IsType<VoidValue>();
        public bool IsInteger() => IsType<IntegerValue>();
        public bool IsBool() => IsType<BoolValue>();
        public bool IsString() => IsType<StringValue>();
        public bool IsFunction() => IsType<FunctionValue>();
        public bool IsNativeFunction() => IsType<NativeFunctionValue>();

        // Típus elérések

        public IntegerValue AsInteger() => AsType<IntegerValue>();
        public BoolValue AsBool() => AsType<BoolValue>();
        public StringValue AsString() => AsType<StringValue>();
        public FunctionValue AsFunction() => AsType<FunctionValue>();
        public NativeFunctionValue AsNativeFunction() => AsType<NativeFunctionValue>();

        // Gyár-függvények

        public static IntegerValue Integer(BigInteger value) => new IntegerValue { Value = value };
        public static BoolValue Bool(bool value) => new BoolValue { Value = value };
        public static StringValue String(string value) => new StringValue { Value = value };
        public static FunctionValue Function(FunctionDefinitionStatement node) => new FunctionValue { Node = node };
        public static NativeFunctionValue NativeFunction(Func<List<Value>, Value> func) => new NativeFunctionValue { Function = func };

        // Operátorok

        public static Value OperatorAdd(Value left, Value right)
        {
            if (left.IsInteger() && right.IsInteger())
            {
                return Integer(left.AsInteger().Value + right.AsInteger().Value);
            }
            if (left.IsInteger() && right.IsString())
            {
                return String(left.AsInteger().Value + right.AsString().Value);
            }
            if (left.IsString() && right.IsInteger())
            {
                return String(left.AsString().Value + right.AsInteger().Value);
            }
            if (left.IsString() && right.IsString())
            {
                return String(left.AsString().Value + right.AsString().Value);
            }
            // TODO: Proper error
            throw new Exception("Can't add types");
        }

        public static Value OperatorSubtract(Value left, Value right)
        {
            return Integer(left.AsInteger().Value - right.AsInteger().Value);
        }

        public static Value OperatorMultiply(Value left, Value right)
        {
            if (left.IsInteger() && right.IsInteger())
            {
                return Integer(left.AsInteger().Value * right.AsInteger().Value);
            }
            if ((left.IsString() && right.IsInteger()) || (left.IsInteger() && right.IsString()))
            {
                var (toRepeat, repeatCount) = left.IsString()
                    ? (left.AsString().Value, right.AsInteger().Value)
                    : (right.AsString().Value, left.AsInteger().Value);
                string result = string.Empty;
                for (int i = 0; i < repeatCount; ++i)
                {
                    result += toRepeat;
                }
                return String(result);
            }
            // TODO: Proper error
            throw new Exception("Can't multiply types");
        }

        public static Value OperatorDivide(Value left, Value right)
        {
            return Integer(left.AsInteger().Value / right.AsInteger().Value);
        }

        public static Value OperatorModulo(Value left, Value right)
        {
            return Integer(left.AsInteger().Value % right.AsInteger().Value);
        }

        public static Value OperatorGreater(Value left, Value right)
        {
            return Bool(left.AsInteger().Value > right.AsInteger().Value);
        }

        public static Value OperatorLess(Value left, Value right)
        {
            return Bool(left.AsInteger().Value < right.AsInteger().Value);
        }

        public static Value OperatorGreaterOrEqual(Value left, Value right)
        {
            return Bool(left.AsInteger().Value >= right.AsInteger().Value);
        }

        public static Value OperatorLessOrEqual(Value left, Value right)
        {
            return Bool(left.AsInteger().Value <= right.AsInteger().Value);
        }

        public static Value OperatorEqual(Value left, Value right)
        {
            if (left.IsInteger() && right.IsInteger())
            {
                return Bool(left.AsInteger().Value == right.AsInteger().Value);
            }
            if (left.IsBool() && right.IsBool())
            {
                return Bool(left.AsBool().Value == right.AsBool().Value);
            }
            if (left.IsString() && right.IsString())
            {
                return Bool(left.AsString().Value == right.AsString().Value);
            }
            // TODO: Proper error
            throw new Exception("Can't equate types");
        }

        public static Value OperatorNotEqual(Value left, Value right)
        {
            var result = OperatorEqual(left, right) as BoolValue;
            result.Value = !result.Value;
            return result;
        }
    }

    /// <summary>
    /// A "nem érték" érték.
    /// </summary>
    public class VoidValue : Value
    {
        /// <summary>
        /// Mivel nem lehet többféle érték, elég egyetlen példány.
        /// </summary>
        public static readonly VoidValue Instance = new VoidValue();

        private VoidValue() { }
    }

    /// <summary>
    /// Egész szám érték.
    /// </summary>
    public class IntegerValue : Value
    {
        public BigInteger Value { get; set; }
    }

    /// <summary>
    /// Logikai érték.
    /// </summary>
    public class BoolValue : Value
    {
        public bool Value { get; set; }
    }

    /// <summary>
    /// Szöveges érték.
    /// </summary>
    public class StringValue : Value
    {
        public string Value { get; set; }
    }

    /// <summary>
    /// Függvény érték.
    /// </summary>
    public class FunctionValue : Value
    {
        /// <summary>
        /// A függvénydefiníció szintaxisfája.
        /// </summary>
        public FunctionDefinitionStatement Node { get; set; }
    }

    /// <summary>
    /// Egy natív, C# függvény érték, mely hívható a kis nyelvünkből. Gyakorlatilag az FFI
    /// (Foreign Function Interface) magja.
    /// </summary>
    public class NativeFunctionValue : Value
    {
        /// <summary>
        /// A C# függvény, amit hívunk.
        /// </summary>
        new public Func<List<Value>, Value> Function { get; set; }
    }
}
