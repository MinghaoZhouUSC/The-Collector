#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Runs only on explicit request, in Play Mode. Test data never modifies saved assets.
[InitializeOnLoad]
public static class BRuntimeChecks
{
    private const string Flag="TheCollector.BChecks";
    private static readonly List<string> results=new List<string>();
    private static int step, restarts;
    private static double next;
    private static float deadline;
    private static string runtimeError;
    static BRuntimeChecks()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,trace,type)=>{
            if(SessionState.GetBool(Flag,false)&&(type==LogType.Exception||type==LogType.Error))runtimeError=message;
        };
    }
    [MenuItem("Tools/The Collector/Run B Integration Checks")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before tests.");
        results.Clear();step=0;restarts=0;runtimeError=null;SessionState.SetBool(Flag,true);
        EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message)
    {
        if(!ok)throw new Exception(message);results.Add("PASS "+message);
    }
    private static void Invoke(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
    private static T Get<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
    private static void Move(PlayerState state,Vector2 point)
    {
        var body=state.GetComponent<Rigidbody2D>();body.position=point;body.linearVelocity=Vector2.zero;body.WakeUp();state.transform.position=point;Physics2D.SyncTransforms();
    }
    private static void Delay(double seconds){next=Time.timeAsDouble+seconds;step++;}
    private static void Tick()
    {
        if(!SessionState.GetBool(Flag,false)||!Application.isPlaying||EditorApplication.isPaused)return;
        if(Time.timeAsDouble<next)return;
        try
        {
            if(runtimeError!=null)throw new Exception("Runtime error: "+runtimeError);
            var round=RoundManager.Instance;var player=UnityEngine.Object.FindFirstObjectByType<PlayerState>();
            if(round==null||player==null){if(step==0)return;throw new Exception("Round or Player missing");}
            var inventory=player.GetComponent<PlayerInventory>();
            var door=UnityEngine.Object.FindFirstObjectByType<VaultDoor>();
            switch(step)
            {
                case 0: Application.runInBackground=true; Delay(1);break;
                case 1:
                    Require(round.State==RoundManager.RoundState.Ready,"Round starts Ready");
                    Require(player.IsInSafeHouse,"Real physics detects spawn inside safe house");
                    Require(UnityEngine.Object.FindObjectsByType<RoundManager>(FindObjectsSortMode.None).Length==1,"Exactly one RoundManager");
                    Require(UnityEngine.Object.FindObjectsByType<ItemFactory>(FindObjectsSortMode.None).Length==1,"Exactly one ItemFactory");
                    int items=UnityEngine.Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None).Length;
                    Require(items>=29&&items<=30,"29 loot items and at most one key spawned");
                    var canvas=UnityEngine.Object.FindFirstObjectByType<RoundScreens>().GetComponentInChildren<Canvas>(true);
                    Require(canvas.sortingOrder>20,"Start/end canvas above backpack");
                    Invoke(round,"SetState",RoundManager.RoundState.Playing);Move(player,new Vector2(0,3));Delay(.3);break;
                case 2:
                    Require(!player.IsInSafeHouse,"Real trigger exit clears safe state (position="+player.transform.position+", body="+player.GetComponent<Rigidbody2D>().position+")");
                    inventory.Add(new ItemData{itemName="Test loot",weight=12,value=60});inventory.Add(new ItemData{isKey=true});
                    Move(player,Vector2.zero);Delay(.3);break;
                case 3:
                    Require(inventory.BankedValue==60&&inventory.TotalWeight==0&&inventory.HasKey,"Real trigger deposits loot once and retains key");
                    inventory.ConsumeKey();door.Interact(player.gameObject);
                    Require(door.CanInteract&&door.GetComponent<Collider2D>().enabled,"No key keeps vault locked");
                    inventory.Add(new ItemData{isKey=true});door.Interact(player.gameObject);
                    Require(!inventory.HasKey&&!door.CanInteract&&!door.GetComponent<Collider2D>().enabled,"Key opens vault and is consumed");
                    Require(Array.TrueForAll(door.GetComponentsInChildren<SpriteRenderer>(),x=>!x.enabled),"All vault graphics hidden");
                    inventory.Add(new ItemData{isKey=true});door.Interact(player.gameObject);Require(inventory.HasKey,"Open door does not consume another key");
                    Move(player,new Vector2(-1.2f,9));Delay(.25);break;
                case 4:
                    Require(player.IsFrozen,"Real trap trigger freezes player");deadline=Get<float>(player,"frozenUntil");
                    player.Freeze(.05f);Require(Get<float>(player,"frozenUntil")>=deadline,"Short freeze does not shorten existing deadline");
                    player.Freeze(2.3f);Require(Get<float>(player,"frozenUntil")>deadline,"Long freeze extends deadline");
                    Delay(2.6);break;
                case 5:
                    Require(!player.IsFrozen,"Freeze expires and standing on trap does not retrigger");
                    Move(player,Vector2.zero);inventory.Add(new ItemData{itemName="Test target",weight=1,value=500});Delay(.3);break;
                case 6:
                    Invoke(round,"TryExtractEarly");Delay(.2);break;
                case 7:
                    Require(round.Won&&round.FinalScore==560,"F-equivalent early extraction banks and wins correctly");
                    Require(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).AnyText("EXTRACTION SUCCESS"),"Success screen visible");
                    Invoke(round,"SetState",RoundManager.RoundState.Playing);Move(player,new Vector2(0,3));Delay(.25);break;
                case 8: Invoke(round,"EndRound");Delay(.2);break;
                case 9:
                    Require(!round.Won&&!round.Extracted&&round.FinalScore==0,"Timeout outside fails even with enough banked value");
                    Require(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).AnyText("You were outside"),"Outside failure reason visible");
                    Invoke(round,"SetState",RoundManager.RoundState.Playing);
                    typeof(PlayerInventory).GetField("bankedValue",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,0);
                    Move(player,Vector2.zero);Delay(.25);break;
                case 10:Invoke(round,"EndRound");Delay(.2);break;
                case 11:
                    Require(!round.Won&&round.Extracted&&round.FinalScore==0,"Timeout inside with insufficient money fails");
                    Require(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).AnyText("You did not bank enough"),"Insufficient money reason visible");
                    typeof(RoundManager).GetMethod("Restart",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Delay(1);break;
                case 12:
                    Require(round.State==RoundManager.RoundState.Ready&&inventory.BankedValue==0&&!player.IsFrozen&&door.CanInteract,"Restart "+(restarts+1)+" resets round, score, freeze and vault");
                    if(++restarts<3){typeof(RoundManager).GetMethod("Restart",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);next=Time.timeAsDouble+1;}
                    else Finish(true);break;
            }
        }
        catch(Exception e){results.Add("FAIL "+e);Finish(false);}
    }
    private static bool AnyText(this Text[] texts,string value)
    {
        foreach(var text in texts)if(text.isActiveAndEnabled&&text.text.Contains(value))return true;return false;
    }
    private static void Finish(bool passed)
    {
        SessionState.SetBool(Flag,false);Directory.CreateDirectory("Temp/BAutomation");
        File.WriteAllLines("Temp/BAutomation/runtime-tests.txt",results);
        File.WriteAllText("Temp/BAutomation/test-result.txt",passed?"PASS":"FAIL");
        Debug.Log("[B tests] "+(passed?"PASS":"FAIL")+"\n"+string.Join("\n",results));
        EditorApplication.isPlaying=false;
    }
}
#endif
