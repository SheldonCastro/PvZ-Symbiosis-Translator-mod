using System;
using System.Collections.Generic;
using UnityEngine;

namespace PvZSymbiosisTranslator.Core;

public readonly struct TextContext
{
    public readonly string ActualScene;
    public readonly string Hierarchy;
    public string ContextKey => ActualScene + "/" + Hierarchy;
    public TextContext(string actualScene, string hierarchy) { ActualScene=actualScene; Hierarchy=hierarchy; }
}

public static class TextContextResolver
{
    public static TextContext Resolve(Component component, string fallbackScene)
    {
        if (component == null || component.gameObject == null) return new TextContext(fallbackScene ?? "", "");
        var actual = component.gameObject.scene;
        var scene = actual.IsValid() && !string.IsNullOrEmpty(actual.name) ? actual.name : fallbackScene ?? "";
        var names = new List<string>();
        for (var node=component.transform; node!=null; node=node.parent) names.Add(node.name);
        names.Reverse();
        return new TextContext(scene, string.Join("/",names));
    }
}
