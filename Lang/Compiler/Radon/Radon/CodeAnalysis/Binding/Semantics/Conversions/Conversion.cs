using System;
using System.Collections.Immutable;
using Radon.CodeAnalysis.Binding.Semantics.Expressions;
using Radon.CodeAnalysis.Symbols;
using Radon.CodeAnalysis.Syntax;
using Radon.CodeAnalysis.Syntax.Nodes.Expressions;

namespace Radon.CodeAnalysis.Binding.Semantics.Conversions;

internal sealed class Conversion
{
    public static readonly Conversion None = new(exists: false, isIdentity: false, isImplicit: false);
    public static readonly Conversion Identity = new(exists: true, isIdentity: true, isImplicit: true);
    public static readonly Conversion Implicit = new(exists: true, isIdentity: false, isImplicit: true);
    public static readonly Conversion Explicit = new(exists: true, isIdentity: false, isImplicit: false);
    
    public bool Exists { get; }
    public bool IsIdentity { get; }
    public bool IsImplicit { get; }
    public bool IsExplicit => Exists && !IsImplicit;
    public Conversion(bool exists, bool isIdentity, bool isImplicit)
    {
        Exists = exists;
        IsIdentity = isIdentity;
        IsImplicit = isImplicit;
    }
    
    public static Conversion Classify(TypeSymbol from, TypeSymbol to)
    {
        if (from == to &&
            from is not TypeParameterSymbol &&
            to is not TypeParameterSymbol)
        {
            return Identity;
        }

        if (from != TypeSymbol.Error && to != TypeSymbol.Error)
        {
            /*TypeParameterSymbol? typeParameter = null;
            TypeSymbol? other = null;
            if (from is TypeParameterSymbol fromTypeParam)
            {
                if (fromTypeParam.TypeMap.IsUnbound(fromTypeParam))
                {
                    return Identity; // this is identity because it is unbound
                }
                
                typeParameter = fromTypeParam;
                other = to;
            }
            else if (to is TypeParameterSymbol toTypeParam)
            {
                if (toTypeParam.TypeMap.IsUnbound(toTypeParam))
                {
                    return Identity; // this is identity because it is unbound
                }
                
                typeParameter = toTypeParam;
                other = from;
            }

            if (typeParameter != null && other != null)
            {
                var bound = typeParameter.TypeMap.GetType(typeParameter);
                return Classify(bound, other);
            }*/

            var fromGeneric = false;
            var toGeneric = false;
            var fromUnbound = false;
            var toUnbound = false;
            var fromType = from;
            var toType = to;
            if (from is not TypeParameterSymbol &&
                to is TypeParameterSymbol t)
            {
                toGeneric = true;
                if (t.TypeMap.IsUnbound(t))
                {
                    toUnbound = true;
                }
                
                toType = ResolveGenericType(to);
            }
            else if (from is TypeParameterSymbol t1 &&
                     to is not TypeParameterSymbol)
            {
                fromGeneric = true;
                if (t1.TypeMap.IsUnbound(t1))
                {
                    fromUnbound = true;
                }
                
                fromType = ResolveGenericType(from);
            }
            
            if (fromUnbound && toUnbound)
            {
                return None;
            }
            
            if (fromType == toType)
            {
                return Identity;
            }

            if (fromGeneric && toGeneric)
            {
                switch (fromUnbound)
                {
                    case true when toUnbound:
                        return Identity;
                    case true when !toUnbound:
                        return Explicit;
                    case false when toUnbound:
                        return Implicit;
                }
            }
            else if (!fromGeneric && toGeneric)
            {
                return Implicit;
            }
            else if (fromGeneric && !toGeneric)
            {
                return Explicit;
            }

            if (IsNumericType(fromType) && IsNumericType(toType))
            {
                return ClassifyNumericConversion(fromType, toType);
            }

            if (IsNumericType(fromType) && toType == TypeSymbol.String)
            {
                return Explicit;
            }
        }

        return None;
    }
    
    public static Conversion Classify(BoundExpression from, TypeSymbol to)
    {
        var fromType = ResolveGenericType(from.Type);
        var toType = ResolveGenericType(to);
        if (fromType == TypeSymbol.Error ||
            toType == TypeSymbol.Error)
        {
            if (fromType == TypeSymbol.Error &&
                toType == TypeSymbol.Error)
            {
                return Classify(from.Type, to);
            }
    
            if (fromType == TypeSymbol.Error)
            {
                return Classify(from.Type, toType);
            }
    
            if (toType == TypeSymbol.Error)
            {
                return Classify(fromType, to);
            }
        }

        if (from is BoundLiteralExpression literal)
        {
            // In these cases, we need to handle cases like:
            // Method<byte>(100) where 100 is a literal of type sbyte, but can be implicitly converted to byte.
            
            // We essentially need to see if the literal falls within the range of the type.
            if (literal.Syntax is LiteralExpressionSyntax literalSyntax &&
                literalSyntax.LiteralToken.Kind == SyntaxKind.NumberToken)
            {
                var value = Convert.ToDouble(literalSyntax.LiteralToken.Value);
                var validTypeConversion = GetTypeRange(value);
                if (validTypeConversion.Contains(toType))
                {
                    return Implicit;
                }

                return Explicit;
            }
        }
        
        return Classify(fromType, toType);
    }

    public static TypeSymbol ResolveGenericType(TypeSymbol type)
    {
        if (type is TypeParameterSymbol typeParameter)
        {
            return ResolveGenericType(typeParameter.TypeMap.GetType(typeParameter));
        }
        
        return type;
    }

    private static ImmutableArray<TypeSymbol> GetTypeRange(double value)
    {
        var validTypes = ImmutableArray.CreateBuilder<TypeSymbol>();
        if (value is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            validTypes.Add(TypeSymbol.SByte);
        }
        
        if (value is >= byte.MinValue and <= byte.MaxValue)
        {
            validTypes.Add(TypeSymbol.Byte);
        }

        if (value is >= short.MinValue and <= short.MaxValue)
        {
            validTypes.Add(TypeSymbol.Short);
        }
        
        if (value is >= ushort.MinValue and <= ushort.MaxValue)
        {
            validTypes.Add(TypeSymbol.UShort);
        }
        
        if (value is >= int.MinValue and <= int.MaxValue)
        {
            validTypes.Add(TypeSymbol.Int);
        }
        
        if (value is >= uint.MinValue and <= uint.MaxValue)
        {
            validTypes.Add(TypeSymbol.UInt);
        }
        
        if (value is >= long.MinValue and <= long.MaxValue)
        {
            validTypes.Add(TypeSymbol.Long);
        }
        
        if (value is >= ulong.MinValue and <= ulong.MaxValue)
        {
            validTypes.Add(TypeSymbol.ULong);
        }
        
        if (value is >= float.MinValue and <= float.MaxValue)
        {
            validTypes.Add(TypeSymbol.Float);
        }
        
        if (value is >= double.MinValue and <= double.MaxValue)
        {
            validTypes.Add(TypeSymbol.Double);
        }

        return validTypes.ToImmutable();
    }

    private static Conversion ClassifyNumericConversion(TypeSymbol from, TypeSymbol to)
    {
        if ((IsUnsignedIntegralType(from) && IsUnsignedIntegralType(to)) ||
            (!IsUnsignedIntegralType(from) && !IsUnsignedIntegralType(to)) ||
            (IsFloatingPointType(from) && IsFloatingPointType(to)))
        {
            var fromRank = GetNumericTypeRank(from);
            var toRank = GetNumericTypeRank(to);
            if (fromRank < toRank)
            {
                return Implicit;
            }
            
            if (fromRank > toRank)
            {
                return Explicit;
            }
            
            return Identity;
        }

        // Here is where it gets tricky.
        // We're dealing with crossed signed conversions.
        // This means we can have a uint to long, a long to uint, an int to uint, a long to ulong, etc.
        // Signed to unsigned conversions are always explicit, if the unsigned type is of the same type.
        // For instance, uint to int is explicit, but uint to long is implicit.
        // So, instead of checking for the rank, we check for the size.
        if ((IsUnsignedIntegralType(from) && !IsUnsignedIntegralType(to)) ||
            (!IsUnsignedIntegralType(from) && IsUnsignedIntegralType(to)))
        {
            var fromSize = GetNumericTypeSize(from);
            var toSize = GetNumericTypeSize(to);
            if (fromSize >= toSize)
            {
                return Explicit;
            }
            
            return Implicit;
        }
        
        return None;
    }

    public static bool IsFloatingPointType(TypeSymbol type)
    {
        return type == TypeSymbol.Float || type == TypeSymbol.Double;
    }

    public static bool IsIntegralType(TypeSymbol type)
    {
        if (IsFloatingPointType(type))
        {
            return false;
        }
        
        var numericTypes = TypeSymbol.GetNumericTypes();
        foreach (var numericType in numericTypes)
        {
            if (type == numericType)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public static bool IsNumericType(TypeSymbol type)
    {
        return IsIntegralType(type) || IsFloatingPointType(type);
    }
    
    public static bool IsUnsignedIntegralType(TypeSymbol type)
    {
        return type == TypeSymbol.Byte || type == TypeSymbol.UShort ||
               type == TypeSymbol.UInt || type == TypeSymbol.ULong;
    }
    
    private static int GetNumericTypeRank(TypeSymbol type)
    {
        if (type == TypeSymbol.SByte)
        {
            return 1;
        }

        if (type == TypeSymbol.Byte)
        {
            return 2;
        }

        if (type == TypeSymbol.Short)
        {
            return 3;
        }

        if (type == TypeSymbol.UShort)
        {
            return 4;
        }

        if (type == TypeSymbol.Int)
        {
            return 5;
        }

        if (type == TypeSymbol.UInt)
        {
            return 6;
        }

        if (type == TypeSymbol.Long)
        {
            return 7;
        }

        if (type == TypeSymbol.ULong)
        {
            return 8;
        }

        if (type == TypeSymbol.Float)
        {
            return 9;
        }

        if (type == TypeSymbol.Double)
        {
            return 10;
        }

        return -1;
    }

    private static int GetNumericTypeSize(TypeSymbol type)
    {
        if (type == TypeSymbol.Byte || type == TypeSymbol.SByte)
        {
            return 1;
        }
        
        if (type == TypeSymbol.Short || type == TypeSymbol.UShort)
        {
            return 2;
        }
        
        if (type == TypeSymbol.Int || type == TypeSymbol.UInt || type == TypeSymbol.Float)
        {
            return 4;
        }
        
        if (type == TypeSymbol.Long || type == TypeSymbol.ULong || type == TypeSymbol.Double)
        {
            return 8;
        }

        return -1;
    }
}