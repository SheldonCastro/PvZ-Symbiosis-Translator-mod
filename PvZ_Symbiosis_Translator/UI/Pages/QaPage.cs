using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using PvZSymbiosisTranslator.QA;
using UnityEngine;
using UnityEngine.Events;

namespace PvZSymbiosisTranslator.UI.Pages;

internal sealed class QaPageBindings
{
    public Func<QaSnapshot> State;
    public Action RunFullQa,ExportReport,ExportUnresolved,OpenExports,CopySummary;
}

internal static class QaPage
{
    private const string Prefix="PvZTranslator.Native.Page.Qa.";
    private const string Summary=Prefix+"Summary";
    private const string Checks=Prefix+"Checks";
    private const string Issues=Prefix+"Issues";

    public static bool Build(GameObject root,GameObject textTemplate,NativeAssetSet assets,string locale,QaPageBindings bindings)
    {
        if(root==null||textTemplate==null||assets?.SmallButtonTemplate==null||assets.BigButtonTemplate==null||bindings==null)return false;
        var old=FindFirstText(root);if(old!=null)old.text="";
        Label(root,textTemplate,"Label.qa.summary","qa.summary",locale,-440f,-12f,520f,42f,25f,true);
        var summary=Label(root,textTemplate,Summary,null,locale,-420f,-120f,540f,180f,18f);ConfigureInfo(summary);
        Label(root,textTemplate,"Label.qa.checks","qa.checks",locale,-440f,-224f,520f,42f,25f,true);
        var checks=Label(root,textTemplate,Checks,null,locale,-420f,-372f,550f,250f,17f);ConfigureInfo(checks);
        Label(root,textTemplate,"Label.qa.issues","qa.issues",locale,150f,-12f,520f,42f,25f,true);
        var issues=Label(root,textTemplate,Issues,null,locale,280f,-120f,600f,180f,17f);ConfigureInfo(issues);
        Label(root,textTemplate,"Label.qa.actions","qa.actions",locale,150f,-224f,520f,42f,25f,true);
        if(!Big(root,assets,Prefix+"Run","qa.run",locale,330f,-278f,bindings.RunFullQa))return false;
        if(!Small(root,assets,Prefix+"Export","qa.exportReport",locale,195f,-350f,bindings.ExportReport))return false;
        if(!Small(root,assets,Prefix+"Unresolved","qa.exportUnresolved",locale,465f,-350f,bindings.ExportUnresolved))return false;
        if(!Small(root,assets,Prefix+"Open","qa.openExports",locale,195f,-414f,bindings.OpenExports))return false;
        if(!Small(root,assets,Prefix+"Copy","qa.copySummary",locale,465f,-414f,bindings.CopySummary))return false;
        Refresh(root,locale,bindings);return true;
    }

    public static void Refresh(GameObject root,string locale,QaPageBindings bindings)
    {
        if(root==null||bindings==null)return;var s=bindings.State?.Invoke();
        if(s==null){Set(root,Summary,ModLabels.Get(locale,"qa.notScanned"));Set(root,Checks,"—");Set(root,Issues,"—");Relabel(root,locale);return;}
        Set(root,Summary,$"{ModLabels.Get(locale,"qa.overall")}: {Status(locale,s.OverallStatus)}\n{ModLabels.Get(locale,"qa.errors")}: {s.Errors}    •    {ModLabels.Get(locale,"qa.warnings")}: {s.Warnings}    •    {ModLabels.Get(locale,"qa.info")}: {s.Information}\n{ModLabels.Get(locale,"qa.exact")}: {s.ExactTranslations}    •    {ModLabels.Get(locale,"qa.dynamic")}: {s.DynamicRules}    •    {ModLabels.Get(locale,"qa.contexts")}: {s.ContextOverrides}\n{ModLabels.Get(locale,"qa.runtime")}: {s.Runtime.RuntimeObserved}    •    {ModLabels.Get(locale,"qa.unknown")}: {s.Runtime.Unknown}\n{ModLabels.Get(locale,"qa.lastScan")}: {s.Timestamp.ToLocalTime():HH:mm:ss}");
        var preferred=new[]{"json","placeholders","markup","dynamic","conflicts","cjk","textures","audio","contamination"};
        Set(root,Checks,string.Join("\n",preferred.Select(id=>s.Checks.FirstOrDefault(x=>x.Id==id)).Where(x=>x!=null).Select(x=>$"{Status(locale,x.Status),-6}  {ModLabels.Get(locale,"qa.check."+x.Id)} ({x.Findings})")));
        var top=s.Issues.Where(x=>x.Severity!=QaSeverity.Info).Take(4).Select(x=>$"[{Severity(locale,x.Severity)}] {ModLabels.Get(locale,"qa.check."+x.Check)}: {Clip(FindingId(x),54)}").ToArray();
        Set(root,Issues,$"{s.Errors} {ModLabels.Get(locale,"qa.errors").ToLowerInvariant()}  •  {s.Warnings} {ModLabels.Get(locale,"qa.warnings").ToLowerInvariant()}  •  {s.Information} {ModLabels.Get(locale,"qa.info").ToLowerInvariant()}\n\n"+(top.Length==0?ModLabels.Get(locale,"qa.noImportantIssues"):string.Join("\n",top)));
        Relabel(root,locale);
    }

    private static string Status(string locale,QaStatus value)=>ModLabels.Get(locale,"qa.status."+(value switch{QaStatus.Pass=>"pass",QaStatus.Warning=>"warn",QaStatus.Fail=>"fail",QaStatus.NotApplicable=>"na",_=>"info"}));
    private static string Severity(string locale,QaSeverity value)=>ModLabels.Get(locale,"qa.status."+(value==QaSeverity.Error?"fail":value==QaSeverity.Warning?"warn":"info"));
    private static string FindingId(QaIssue issue){var value=string.IsNullOrWhiteSpace(issue.Source)?issue.File:issue.Source;return string.IsNullOrWhiteSpace(value)?issue.Check:string.Join(" ",(value??"").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries));}
    private static string Clip(string value,int max)=>string.IsNullOrEmpty(value)||value.Length<=max?value:value.Substring(0,max)+"…";
    private static void ConfigureInfo(TMP_Text text){if(text==null)return;text.alignment=TextAlignmentOptions.TopLeft;text.lineSpacing=2f;}
    private static bool Big(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action action)=>Button(root,assets.BigButtonTemplate,name,key,locale,x,y,390f,62f,action,17f,25f);
    private static bool Small(GameObject root,NativeAssetSet assets,string name,string key,string locale,float x,float y,Action action)=>Button(root,assets.SmallButtonTemplate,name,key,locale,x,y,245f,52f,action,13f,19f);
    private static bool Button(GameObject root,GameObject template,string name,string key,string locale,float x,float y,float width,float height,Action action,float min,float max){UnityAction click=action;var b=NativeUiFactory.CloneNativeButton(template,root.transform,name,click);if(b==null)return false;Place(b.Root,x,y,width,height);var rect=b.Label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width-18f,height-8f);b.Label.enableAutoSizing=true;b.Label.fontSizeMin=min;b.Label.fontSizeMax=max;b.Label.fontSize=max;b.Label.color=new Color(.96f,.88f,.68f,1f);b.Label.text=ModLabels.Get(locale,key);return true;}
    private static TMP_Text Label(GameObject root,GameObject template,string name,string key,string locale,float x,float y,float width,float height,float size,bool heading=false){var clone=UnityEngine.Object.Instantiate<GameObject>(template,root.transform,false);clone.name=name;var text=FindFirstText(clone);if(text!=null){var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;text.enableAutoSizing=true;text.fontSizeMin=heading?17f:12f;text.fontSizeMax=size;text.fontSize=size;text.alignment=TextAlignmentOptions.Left;text.color=new Color(.22f,.105f,.035f,1f);text.raycastTarget=false;text.text=key==null?"":ModLabels.Get(locale,key);}clone.SetActive(true);return text;}
    private static void Relabel(GameObject root,string locale){for(var i=0;i<root.transform.childCount;i++){var child=root.transform.GetChild(i)?.gameObject;if(child==null||!child.name.StartsWith("Label.",StringComparison.Ordinal))continue;var text=FindFirstText(child);if(text!=null)text.text=ModLabels.Get(locale,child.name.Substring(6));}}
    private static void Set(GameObject root,string name,string value){var text=FindFirstText(Find(root.transform,name));if(text!=null)text.text=value;}
    private static void Place(GameObject target,float x,float y,float width,float height){var rect=target.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;target.SetActive(true);}
    private static GameObject Find(Transform parent,string name){if(parent==null)return null;for(var i=0;i<parent.childCount;i++){var child=parent.GetChild(i);if(child!=null&&child.gameObject.name==name)return child.gameObject;}return null;}
    private static TMP_Text FindFirstText(GameObject root){if(root==null)return null;var stack=new Stack<Transform>();stack.Push(root.transform);while(stack.Count>0){var node=stack.Pop();var text=node.gameObject.GetComponent(Il2CppType.Of<TMP_Text>())?.TryCast<TMP_Text>();if(text!=null)return text;for(var i=node.childCount-1;i>=0;i--)stack.Push(node.GetChild(i));}return null;}
}
