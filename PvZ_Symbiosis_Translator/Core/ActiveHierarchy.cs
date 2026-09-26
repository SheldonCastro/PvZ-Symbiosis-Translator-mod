using System.Collections.Generic;
using UnityEngine;
using Il2CppInterop.Runtime;
namespace PvZSymbiosisTranslator.Core;

// Per-object component lookup avoids the IL2CPP generic array traversal overload.
internal static class ActiveHierarchy
{
    public static IEnumerable<T> Components<T>(GameObject root) where T : Component
    {
        if(root==null) yield break;
        var stack=new Stack<Transform>(); stack.Push(root.transform);
        while(stack.Count>0) {
            var node=stack.Pop();
            if(node==null || !node.gameObject.activeInHierarchy || PvZSymbiosisTranslatorMod.IsTranslatorOwned(node.gameObject)) continue;
            var component=node.gameObject.GetComponent(Il2CppType.Of<T>())?.TryCast<T>();
            if(component!=null) yield return component;
            for(int i=node.childCount-1;i>=0;i--) { var child=node.GetChild(i); if(child!=null) stack.Push(child); }
        }
    }
}
