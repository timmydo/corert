// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// This file defines an internal class used to throw exceptions in BCL code.
// The main purpose is to reduce code size. 
// 
// The old way to throw an exception generates quite a lot IL code and assembly code.
// Following is an example:
//     C# source
//          throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
//     IL code:
//          IL_0003:  ldstr      "key"
//          IL_0008:  ldstr      "ArgumentNull_Key"
//          IL_000d:  call       string System.Environment::GetResourceString(string)
//          IL_0012:  newobj     instance void System.ArgumentNullException::.ctor(string,string)
//          IL_0017:  throw
//    which is 21bytes in IL.
// 
// So we want to get rid of the ldstr and call to Environment.GetResource in IL.
// In order to do that, I created two enums: ExceptionResource, ExceptionArgument to represent the
// argument name and resource name in a small integer. The source code will be changed to 
//    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key, ExceptionResource.ArgumentNull_Key);
//
// The IL code will be 7 bytes.
//    IL_0008:  ldc.i4.4
//    IL_0009:  ldc.i4.4
//    IL_000a:  call       void System.ThrowHelper::ThrowArgumentNullException(valuetype System.ExceptionArgument)
//    IL_000f:  ldarg.0
//
// This will also reduce the Jitted code size a lot. 
//
// It is very important we do this for generic classes because we can easily generate the same code 
// multiple times for different instantiation. 
// 

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System
{
    [Pure]
    internal static class ThrowHelper
    {
        internal static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        internal static void ThrowInvalidTypeWithPointersNotSupported(Type targetType)
        {
            throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeWithPointersNotSupported, targetType));
        }

        internal static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        internal static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
        {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
        {
            throw GetArgumentOutOfRangeException(argument, resource);
        }
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
        }

        internal static void ThrowArgumentException_DestinationTooShort()
        {
            throw new ArgumentException(SR.Argument_DestinationTooShort);
        }
        internal static void ThrowArgumentOutOfRange_IndexException()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index,
                                                    ExceptionResource.ArgumentOutOfRange_Index);
        }
        internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index,
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowArgumentException(ExceptionResource resource)
        {
            throw new ArgumentException(GetResourceString(resource));
        }

        internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument)
        {
            throw GetArgumentException(resource, argument);
        }
        private static ArgumentException GetArgumentException(ExceptionResource resource, ExceptionArgument argument)
        {
            return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
        }

        internal static void ThrowArgumentException_Argument_InvalidArrayType()
        {
            throw new ArgumentException(GetResourceString(ExceptionResource.Argument_InvalidArrayType));
        }

        private static ArgumentException GetWrongValueTypeArgumentException(object value, Type targetType)
        {
            return new ArgumentException(SR.Format(SR.Arg_WrongType, value, targetType), nameof(value));
        }

        internal static void ThrowWrongValueTypeArgumentException(object value, Type targetType)
        {
            throw GetWrongValueTypeArgumentException(value, targetType);
        }

        internal static void ThrowArgumentNullException(ExceptionArgument argument)
        {
            throw new ArgumentNullException(GetArgumentName(argument));
        }

        internal static void ThrowObjectDisposedException(string objectName, ExceptionResource resource)
        {
            throw new ObjectDisposedException(objectName, GetResourceString(resource));
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource)
        {
            throw new InvalidOperationException(GetResourceString(resource));
        }

        internal static void ThrowNotSupportedException(ExceptionResource resource)
        {
            throw new NotSupportedException(GetResourceString(resource));
        }

        // Allow nulls for reference types and Nullable<U>, but not for value types.
        // Aggressively inline so the jit evaluates the if in place and either drops the call altogether
        // Or just leaves null test and call to the Non-returning ThrowHelper.ThrowArgumentNullException
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IfNullAndNullsAreIllegalThenThrow<T>(object value, ExceptionArgument argName)
        {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            if (!(default(T) == null) && value == null)
                ThrowHelper.ThrowArgumentNullException(argName);
        }

        private static string GetArgumentName(ExceptionArgument argument)
        {
            switch (argument)
            {
                case ExceptionArgument.array:
                    return "array";
                case ExceptionArgument.text:
                    return "text";
                case ExceptionArgument.values:
                    return "values";
                case ExceptionArgument.obj:
                    return "obj";
                case ExceptionArgument.value:
                    return "value";
                case ExceptionArgument.startIndex:
                    return "startIndex";
                case ExceptionArgument.task:
                    return "task";
                case ExceptionArgument.s:
                    return "s";
                case ExceptionArgument.input:
                    return "input";
                case ExceptionArgument.ownedMemory:
                    return "ownedMemory";
                case ExceptionArgument.list:
                    return "list";
                case ExceptionArgument.index:
                    return "index";
                default:
                    Debug.Assert(false,
                        "The enum value is not defined, please check the ExceptionArgument Enum.");
                    return "";
            }
        }

        private static string GetResourceString(ExceptionResource resource)
        {
            switch (resource)
            {
                case ExceptionResource.ArgumentOutOfRange_Index:
                    return SR.ArgumentOutOfRange_Index;
                case ExceptionResource.Arg_ArrayPlusOffTooSmall:
                    return SR.Arg_ArrayPlusOffTooSmall;
                case ExceptionResource.Memory_ThrowIfDisposed:
                    return SR.Memory_ThrowIfDisposed;
                case ExceptionResource.Memory_OutstandingReferences:
                    return SR.Memory_OutstandingReferences;
                case ExceptionResource.NotSupported_ReadOnlyCollection:
                    return SR.NotSupported_ReadOnlyCollection;
                case ExceptionResource.Arg_RankMultiDimNotSupported:
                    return SR.Arg_RankMultiDimNotSupported;
                case ExceptionResource.Arg_NonZeroLowerBound:
                    return SR.Arg_NonZeroLowerBound;
                case ExceptionResource.ArgumentOutOfRange_ListInsert:
                    return SR.ArgumentOutOfRange_ListInsert;
                case ExceptionResource.Argument_InvalidArrayType:
                    return SR.Argument_InvalidArrayType;
                case ExceptionResource.ArgumentOutOfRange_NeedNonNegNum:
                    return SR.ArgumentOutOfRange_NeedNonNegNum;
                default:
                    Debug.Assert(false,
                        "The enum value is not defined, please check the ExceptionResource Enum.");
                    return "";
            }
        }
    }

    //
    // The convention for this enum is using the argument name as the enum name
    // 
    internal enum ExceptionArgument
    {
        array,
        text,
        values,
        obj,
        value,
        startIndex,
        task,
        s,
        input,
        ownedMemory,
        list,
        index,
    }

    //
    // The convention for this enum is using the resource name as the enum name
    // 
    internal enum ExceptionResource
    {
        ArgumentOutOfRange_Index,
        Arg_ArrayPlusOffTooSmall,
        Memory_ThrowIfDisposed,
        Memory_OutstandingReferences,
        NotSupported_ReadOnlyCollection,
        Arg_RankMultiDimNotSupported,
        Arg_NonZeroLowerBound,
        ArgumentOutOfRange_ListInsert,
        Argument_InvalidArrayType,
        ArgumentOutOfRange_NeedNonNegNum,
    }
}
