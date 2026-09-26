using System;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PvZSymbiosisTranslator.UI;

internal sealed class NativeButtonClone
{
    public GameObject Root;
    public Button Button;
    public TMP_Text Label;
    public int OriginalPersistentListeners;
    public int ListenersAfterRebind;
    public string Hierarchy;
}

internal sealed class NativeImageButtonClone
{
    public GameObject Root;
    public Button Button;
    public Image Image;
}

internal static class NativeUiFactory
{
    public static NativeButtonClone CloneNativeButton(GameObject template, Transform parent, string objectName, UnityAction callback)
    {
        if(template==null || parent==null || callback==null) return null;
        if(ContainsForbiddenComponent(template)) {
            MelonLogger.Error("[NativeUI] template rejected: forbidden Canvas component");
            return null;
        }

        GameObject clone=null;
        try
        {
            clone=UnityEngine.Object.Instantiate<GameObject>(template,parent,false);
            clone.name=objectName;
            if(ContainsForbiddenComponent(clone)) {
                MelonLogger.Error("[NativeUI] clone rejected: Canvas/CanvasScaler/GraphicRaycaster found");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            StripUnexpectedBehaviours(clone);
            var button=Get<Button>(clone);
            var label=FindFirst<TMP_Text>(clone);
            if(button==null || label==null) {
                MelonLogger.Error("[NativeUI] clone rejected: native Button/TMP label missing");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            var originalListeners=button.onClick?.GetPersistentEventCount() ?? 0;
            button.onClick=new Button.ButtonClickedEvent();
            button.onClick.AddListener(callback);
            button.interactable=true;

            var gameCanvas=FindAncestorCanvas(parent);
            if(gameCanvas==null) {
                MelonLogger.Error("[NativeUI] clone rejected: no game-owned ancestor Canvas");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            return new NativeButtonClone {
                Root=clone, Button=button, Label=label,
                OriginalPersistentListeners=originalListeners,
                ListenersAfterRebind=1,
                Hierarchy=BuildHierarchy(clone.transform)
            };
        }
        catch(Exception ex)
        {
            if(clone!=null) try { UnityEngine.Object.Destroy(clone); } catch { }
            MelonLogger.Error("[NativeUI] native button clone failed: "+ex.Message);
            return null;
        }
    }

    public static NativeImageButtonClone CloneNativeImageButton(GameObject template,Transform parent,string objectName,UnityAction callback)
    {
        var clone=CloneNativeVisual(template,parent,objectName);
        if(clone==null || callback==null) return null;
        var button=Get<Button>(clone);
        var image=Get<Image>(clone);
        if(button==null || image==null) {
            MelonLogger.Error("[NativeUI] image button clone rejected: native Button/Image missing");
            UnityEngine.Object.Destroy(clone);
            return null;
        }
        button.onClick=new Button.ButtonClickedEvent();
        button.onClick.AddListener(callback);
        button.enabled=true;
        button.interactable=true;
        image.enabled=true;
        image.raycastTarget=true;
        return new NativeImageButtonClone {Root=clone,Button=button,Image=image};
    }

    public static GameObject CloneNativeVisual(GameObject template,Transform parent,string objectName)
    {
        if(template==null || parent==null || ContainsForbiddenComponent(template)) return null;
        GameObject clone=null;
        try
        {
            clone=UnityEngine.Object.Instantiate<GameObject>(template,parent,false);
            clone.name=objectName;
            if(ContainsForbiddenComponent(clone)) {
                UnityEngine.Object.Destroy(clone);
                return null;
            }
            StripUnexpectedBehaviours(clone);
            if(FindAncestorCanvas(parent)==null) {
                UnityEngine.Object.Destroy(clone);
                return null;
            }
            return clone;
        }
        catch(Exception ex)
        {
            if(clone!=null) try { UnityEngine.Object.Destroy(clone); } catch { }
            MelonLogger.Error("[NativeUI] native visual clone failed: "+ex.Message);
            return null;
        }
    }

    public static bool ContainsForbiddenComponent(GameObject root)
    {
        if(root==null) return false;
        var stack=new System.Collections.Generic.Stack<Transform>();
        stack.Push(root.transform);
        while(stack.Count>0) {
            var node=stack.Pop();
            if(node==null) continue;
            if(node.gameObject.GetComponent(Il2CppType.Of<Canvas>())!=null ||
               node.gameObject.GetComponent(Il2CppType.Of<CanvasScaler>())!=null ||
               node.gameObject.GetComponent(Il2CppType.Of<GraphicRaycaster>())!=null) return true;
            for(int i=node.childCount-1;i>=0;i--) stack.Push(node.GetChild(i));
        }
        return false;
    }

    private static void StripUnexpectedBehaviours(GameObject root)
    {
        var stack=new System.Collections.Generic.Stack<Transform>();
        stack.Push(root.transform);
        while(stack.Count>0) {
            var node=stack.Pop();
            if(node==null) continue;
            var behaviours=node.gameObject.GetComponents(Il2CppType.Of<MonoBehaviour>());
            foreach(var component in behaviours) {
                if(component==null) continue;
                if(component.TryCast<Button>()!=null || component.TryCast<Image>()!=null || component.TryCast<TMP_Text>()!=null || component.TryCast<Slider>()!=null) continue;
                var type=component.GetType().FullName;
                MelonLogger.Warning("[NativeUI] stripped unexpected cloned behaviour: "+type);
                UnityEngine.Object.Destroy(component);
            }
            for(int i=node.childCount-1;i>=0;i--) stack.Push(node.GetChild(i));
        }
    }

    private static Canvas FindAncestorCanvas(Transform node)
    {
        while(node!=null) {
            var canvas=node.gameObject.GetComponent(Il2CppType.Of<Canvas>())?.TryCast<Canvas>();
            if(canvas!=null) return canvas;
            node=node.parent;
        }
        return null;
    }

    private static T FindFirst<T>(GameObject root) where T:Component
    {
        var stack=new System.Collections.Generic.Stack<Transform>(); stack.Push(root.transform);
        while(stack.Count>0) {
            var node=stack.Pop(); if(node==null) continue;
            var component=Get<T>(node.gameObject); if(component!=null) return component;
            for(int i=node.childCount-1;i>=0;i--) stack.Push(node.GetChild(i));
        }
        return null;
    }

    private static T Get<T>(GameObject value) where T:Component
        => value?.GetComponent(Il2CppType.Of<T>())?.TryCast<T>();

    private static string BuildHierarchy(Transform node)
    {
        var parts=new System.Collections.Generic.List<string>();
        while(node!=null) { parts.Add(node.gameObject.name); node=node.parent; }
        parts.Reverse(); return string.Join("/",parts);
    }
}
