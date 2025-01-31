using System;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Extensions
{
    public static class TypeDescriptorExtensions
    {
        public static bool IsLeafType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor.Kind == TypeKind.LeafType;

        public static bool IsEntityType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor.Kind == TypeKind.EntityType;

        public static bool IsDataType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor.Kind == TypeKind.DataType ||
            typeDescriptor.Kind == TypeKind.ComplexDataType;

        public static bool ContainsEntity(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor switch
            {
                ListTypeDescriptor listTypeDescriptor =>
                    listTypeDescriptor.InnerType.ContainsEntity(),
                ComplexTypeDescriptor namedTypeDescriptor =>
                    namedTypeDescriptor.Properties
                        .Any(prop => prop.Type.IsEntityType() || prop.Type.ContainsEntity()),
                NonNullTypeDescriptor nonNullTypeDescriptor =>
                    nonNullTypeDescriptor.InnerType.ContainsEntity(),
                _ => false
            };
        }

        public static bool IsOrContainsEntityType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor switch
            {
                ListTypeDescriptor listTypeDescriptor =>
                    listTypeDescriptor.InnerType.IsOrContainsEntityType(),
                ComplexTypeDescriptor namedTypeDescriptor =>
                    namedTypeDescriptor.Properties
                        .Any(prop => prop.Type.IsEntityType() ||
                            prop.Type.IsOrContainsEntityType()) ||
                    namedTypeDescriptor.IsEntityType(),
                NonNullTypeDescriptor nonNullTypeDescriptor =>
                    nonNullTypeDescriptor.InnerType.IsOrContainsEntityType(),
                _ => false
            };
        }

        public static bool IsInterface(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is InterfaceTypeDescriptor;

        public static bool IsNullableType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is not NonNullTypeDescriptor;

        public static bool IsNonNullableType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is NonNullTypeDescriptor;

        public static bool IsListType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is ListTypeDescriptor ||
            typeDescriptor is NonNullTypeDescriptor { InnerType: ListTypeDescriptor };

        public static ITypeDescriptor InnerType(this ITypeDescriptor typeDescriptor)
        {
            if (typeDescriptor is NonNullTypeDescriptor n)
            {
                return n.InnerType;
            }

            if (typeDescriptor is ListTypeDescriptor l)
            {
                return l.InnerType;
            }

            return typeDescriptor;
        }

        public static ITypeDescriptor NamedType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor
                .InnerType()
                .InnerType()
                .InnerType()
                .InnerType()
                .InnerType()
                .InnerType();
        }

        public static NameString GetName(this ITypeDescriptor typeDescriptor)
        {
            if (typeDescriptor.NamedType() is INamedTypeDescriptor namedType)
            {
                return namedType.Name;
            }

            throw new InvalidOperationException("Invalid type structure.");
        }

        public static RuntimeTypeInfo GetRuntimeType(this ITypeDescriptor typeDescriptor)
        {
            if (typeDescriptor.NamedType() is INamedTypeDescriptor namedType)
            {
                return namedType.RuntimeType;
            }

            throw new InvalidOperationException("Invalid type structure.");
        }
    }
}
