using System.Collections.Immutable;
using Radon.CodeAnalysis.Emit.Binary.MetadataBinary;

namespace Radon.Runtime.RuntimeInfo;

internal sealed class TypeReferenceInfo
{
    public TypeInfo TypeDefinition { get; }
    public MemberReferenceInfo ConstructorReference { get; }
    public ImmutableArray<TypeInfo> GenericArguments { get; }
    public TypeReferenceInfo(TypeReference typeReference, Metadata metadata)
    {
        var definition = metadata.Types.Types[typeReference.TypeDefinition];
        TypeDefinition = TypeTracker.Add(definition, metadata, null);
        var memberRef = metadata.MemberReferences.MemberReferences[typeReference.ConstructorReference];
        ConstructorReference = new MemberReferenceInfo(memberRef, metadata);
        var genericArguments = ImmutableArray.CreateBuilder<TypeInfo>();
        foreach (var genericArgument in typeReference.GenericArguments)
        {
            var genericArgumentType = metadata.Types.Types[genericArgument];
            genericArguments.Add(TypeTracker.Add(genericArgumentType, metadata, null));
        }
        
        GenericArguments = genericArguments.ToImmutable();
    }
}