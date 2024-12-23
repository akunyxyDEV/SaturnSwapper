module;

#include "Saturn/Defines.h"

export module Saturn.Properties.DelegateProperty;

export import Saturn.Reflection.FProperty;
import Saturn.Delegates.MulticastScriptDelegate;

import Saturn.Readers.FArchive;

export class FDelegateProperty : public FProperty {
public:
    struct Value : public IPropValue {
    public:
        FScriptDelegate Delegate;

        __forceinline bool IsAcceptableType(EPropertyType Type) override {
            return Type == EPropertyType::DelegateProperty;
        }

        __forceinline void PlaceValue(EPropertyType Type, void* OutBuffer) override {
            if (Type == EPropertyType::DelegateProperty) {
                memcpy(OutBuffer, &Delegate, sizeof(Delegate));
            }
            else if (Type == EPropertyType::StrProperty) {
                *((std::string*)OutBuffer) = Delegate.GetFunctionName();
            }
        }

        void Write(FArchive& Ar, ESerializationMode SerializationMode = ESerializationMode::Normal) override {}
    };

    TUniquePtr<class IPropValue> Serialize(FArchive& Ar) override {
        auto Ret = std::make_unique<Value>();
        Ar << Ret->Delegate;
        
        return std::move(Ret);
    }
};

export class FMulticastDelegateProperty : public FProperty {
public:
    struct Value : public IPropValue {
    public:
        FMulticastScriptDelegate Delegate;

        __forceinline bool IsAcceptableType(EPropertyType Type) override {
            return Type == EPropertyType::MulticastDelegateProperty;
        }

        __forceinline void PlaceValue(EPropertyType Type, void* OutBuffer) override {
            if (Type == EPropertyType::MulticastDelegateProperty) {
                *((FMulticastScriptDelegate*)OutBuffer) = Delegate;
            }
        }

        void Write(FArchive& Ar, ESerializationMode SerializationMode = ESerializationMode::Normal) override {}
    };

    TUniquePtr<class IPropValue> Serialize(FArchive& Ar) override {
        auto Ret = std::make_unique<Value>();
        Ar << Ret->Delegate;
        
        return std::move(Ret);
    }
};