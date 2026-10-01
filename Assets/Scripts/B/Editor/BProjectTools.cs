#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// B-owned authoring tools. Creates assets through Unity APIs, never edits A assets.
[InitializeOnLoad]
public static class BProjectTools
{
    private const string Main = "Assets/Scenes/Main.unity";
    private const string Test = "Assets/Scenes/B_Test.unity";
    private static readonly string Control = Path.GetFullPath("Temp/BAutomation");
    private static Sprite square, circle;
    private static Transform floors, walls, markings;
    private static double nextStatus;
    private static string lastError = "";
    static BProjectTools()
    {
        EditorApplication.update += Poll;
        Application.logMessageReceived += (text, trace, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                lastError = text + "\n" + trace;
        };
    }

    private static void Poll()
    {
        if (!File.Exists(Path.Combine(Control, "enabled.txt"))) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Directory.CreateDirectory(Control);
        string request = Path.Combine(Control, "command.txt");
        if (File.Exists(request))
        {
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                switch (command)
                {
                    case "assemble": Assemble(); break;
                    case "play": EditorApplication.isPlaying = true; break;
                    case "stop": EditorApplication.isPlaying = false; break;
                    case "build": BuildWeb(); break;
                    case "test": BRuntimeChecks.Begin(); break;
                    case "capture": if (Application.isPlaying) ScreenCapture.CaptureScreenshot(Path.Combine(Control, "game.png")); break;
                    default: throw new Exception("Unknown B command: " + command);
                }
                File.WriteAllText(Path.Combine(Control, "result.txt"), "OK " + command + " " + DateTime.UtcNow.ToString("O"));
            }
            catch (Exception e)
            {
                lastError = e.ToString();
                File.WriteAllText(Path.Combine(Control, "result.txt"), "FAIL " + command + "\n" + e);
                Debug.LogException(e);
            }
        }
        if (EditorApplication.timeSinceStartup < nextStatus) return;
        nextStatus = EditorApplication.timeSinceStartup + 1;
        var round = RoundManager.Instance;
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerState>();
        File.WriteAllText(Path.Combine(Control, "status.txt"),
            "scene=" + SceneManager.GetActiveScene().path + "\nplaying=" + Application.isPlaying +
            "\nround=" + (round == null ? "none" : round.State.ToString()) +
            "\nplayer=" + (player == null ? "none" : player.transform.position.ToString()) +
            "\nsafe=" + (player != null && player.IsInSafeHouse) + "\nerror=" + lastError);
    }

    [MenuItem("Tools/The Collector/Assemble B Scenes")]
    public static void Assemble()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode before assembling.");
        if (SceneManager.GetActiveScene().isDirty)
            throw new Exception("Current scene has unsaved changes. Save or discard them before assembling.");
        if (File.Exists(Main) || File.Exists(Test))
            throw new Exception("B scenes already exist. This tool will not overwrite them automatically.");
        Directory.CreateDirectory("Assets/Prefabs/B");
        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
        circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Circle.png");
        if (square == null || circle == null) throw new Exception("A shape sprites missing.");
        CreatePrefabs();
        CreateMap(Main, false);
        CreateMap(Test, true);
        EditorSceneManager.OpenScene(Main);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Main, true) };
        PlayerSettings.productName = "The Collector";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        AssetDatabase.SaveAssets();
        Debug.Log("[B] Main, B_Test and four B prefabs created; Main configured as build scene.");
    }

    private static void CreatePrefabs()
    {
        var safe = new GameObject("SafeHouse");
        var trigger = safe.AddComponent<BoxCollider2D>(); trigger.size = new Vector2(4, 3); trigger.isTrigger = true;
        safe.AddComponent<SafeHouseZone>();
        SpriteShape("Floor", safe.transform, Vector2.zero, new Vector2(4, 3), new Color(.08f,.32f,.26f), -5);
        Color mint = new Color(.35f,.92f,.68f);
        SpriteShape("Top", safe.transform, new Vector2(0,1.5f), new Vector2(4,.08f), mint, -4);
        SpriteShape("Bottom", safe.transform, new Vector2(0,-1.5f), new Vector2(4,.08f), mint, -4);
        SpriteShape("Left", safe.transform, new Vector2(-2,0), new Vector2(.08f,3), mint, -4);
        SpriteShape("Right", safe.transform, new Vector2(2,0), new Vector2(.08f,3), mint, -4);
        SavePrefab(safe, "SafeHouse");

        var door = new GameObject("VaultDoor");
        var solid = door.AddComponent<BoxCollider2D>(); solid.size = new Vector2(.55f,3);
        SpriteShape("Door", door.transform, Vector2.zero, solid.size, new Color(.88f,.64f,.22f), 0);
        for (int i=-2;i<=2;i++) SpriteShape("LockStripe",door.transform,new Vector2(0,i*.5f),new Vector2(.58f,.10f),new Color(.35f,.24f,.10f),1);
        door.AddComponent<VaultDoor>();
        SavePrefab(door, "VaultDoor");

        var trap = new GameObject("Trap");
        var hazard = trap.AddComponent<CircleCollider2D>(); hazard.radius=.55f; hazard.isTrigger=true;
        var image = SpriteShape("Warning",trap.transform,Vector2.zero,new Vector2(1.1f,1.1f),new Color(.9f,.25f,.2f),0,circle);
        SpriteShape("CrossA",trap.transform,Vector2.zero,new Vector2(.7f,.10f),new Color(.18f,.06f,.07f),1).transform.localRotation=Quaternion.Euler(0,0,45);
        SpriteShape("CrossB",trap.transform,Vector2.zero,new Vector2(.7f,.10f),new Color(.18f,.06f,.07f),1).transform.localRotation=Quaternion.Euler(0,0,-45);
        var script=trap.AddComponent<Trap>();
        var so=new SerializedObject(script);so.FindProperty("graphic").objectReferenceValue=image;so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(trap,"Trap");
        var screens=new GameObject("RoundScreens"); screens.AddComponent<RoundScreens>();SavePrefab(screens,"RoundScreens");
    }

    private static void SavePrefab(GameObject go,string name)
    {
        PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefabs/B/"+name+".prefab");
        UnityEngine.Object.DestroyImmediate(go);
    }
    private static Transform Group(string name,Transform parent=null)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
    }
    private static GameObject Prefab(string folder,string name,Vector2 position,Transform parent=null)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+folder+"/"+name+".prefab");
        if(asset==null)throw new Exception("Missing prefab "+name);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.SetParent(parent,false);go.transform.position=position;return go;
    }
    private static SpriteRenderer SpriteShape(string name,Transform parent,Vector2 pos,Vector2 size,Color color,int order,Sprite sprite=null)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite==null?square:sprite;sr.color=color;sr.sortingOrder=order;
        Vector3 native=sr.sprite.bounds.size;go.transform.localScale=new Vector3(size.x/native.x,size.y/native.y,1);
        return sr;
    }
    private static void Wall(string name,Vector2 center,Vector2 size)
    {
        var root=Group(name,walls);root.position=center;
        var col=root.gameObject.AddComponent<BoxCollider2D>();col.size=size;
        SpriteShape("Body",root,Vector2.zero,size,new Color(.29f,.37f,.45f),0);
        SpriteShape("Highlight",root,new Vector2(0,size.y*.28f),new Vector2(size.x,.06f),new Color(.45f,.56f,.64f),0);
    }
    private static void Room(string name,Vector2 c,Color color,bool north,bool south,bool west,bool east)
    {
        SpriteShape(name+"Floor",floors,c,new Vector2(12,10),color,-10);
        Edge(name+"N",c+new Vector2(0,5),true,12,north);
        Edge(name+"S",c-new Vector2(0,5),true,12,south);
        Edge(name+"W",c-new Vector2(6,0),false,10,west);
        Edge(name+"E",c+new Vector2(6,0),false,10,east);
    }
    private static void Edge(string name,Vector2 center,bool horizontal,float length,bool open)
    {
        if(!open) {Wall(name,center,horizontal?new Vector2(length,.5f):new Vector2(.5f,length));return;}
        float segment=(length-3)/2;
        for(int sign=-1;sign<=1;sign+=2)
        {
            Vector2 offset=horizontal?new Vector2(sign*(1.5f+segment/2),0):new Vector2(0,sign*(1.5f+segment/2));
            Wall(name+sign,center+offset,horizontal?new Vector2(segment,.5f):new Vector2(.5f,segment));
        }
    }
    private static void Corridor(Vector2 center,bool vertical)
    {
        SpriteShape("Connector",floors,center,vertical?new Vector2(3,1.5f):new Vector2(1.5f,3),new Color(.12f,.17f,.21f),-10);
        for(int sign=-1;sign<=1;sign+=2)
            Wall("ConnectorWall",center+(vertical?new Vector2(sign*1.75f,0):new Vector2(0,sign*1.75f)),vertical?new Vector2(.5f,1.5f):new Vector2(1.5f,.5f));
    }
    private static void Sign(string words,Vector2 pos,Color color,float width=8)
    {
        var go=new GameObject(words,typeof(RectTransform),typeof(Canvas));go.transform.SetParent(markings,false);go.transform.position=pos;go.transform.localScale=Vector3.one*.01f;
        var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=-2;
        var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(width*100,65);
        var text=UIBuilder.CreateText(go.transform,"Label",words,35,TextAnchor.MiddleCenter,color,false);UIBuilder.Stretch(text.rectTransform);
    }
    private static void Spawner(string name,Vector2 center,string[] items,float[] weights,Vector2[] points)
    {
        var go=Prefab("A","ItemSpawner",center);go.name=name;
        while(go.transform.childCount>0)UnityEngine.Object.DestroyImmediate(go.transform.GetChild(0).gameObject);
        foreach(var point in points) {var p=Group("SpawnPoint",go.transform);p.localPosition=point;}
        var so=new SerializedObject(go.GetComponent<ItemSpawner>());so.FindProperty("count").intValue=points.Length;
        var entries=so.FindProperty("pool.entries");entries.arraySize=items.Length;
        for(int i=0;i<items.Length;i++)
        {
            var entry=entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("item").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/Item_"+items[i]+".asset");
            entry.FindPropertyRelative("weight").floatValue=weights[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<ItemSpawner>());
    }
    private static void CreateMap(string path,bool test)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var world=Group("World");floors=Group("Floors",world);walls=Group("Walls",world);markings=Group("Wayfinding",world);
        Room("Hall",Vector2.zero,new Color(.10f,.15f,.19f),true,true,true,true);
        Room("North",new Vector2(0,11),new Color(.17f,.17f,.14f),false,true,false,false);
        Room("South",new Vector2(0,-11),new Color(.10f,.17f,.23f),true,false,false,false);
        Room("West",new Vector2(-13,0),new Color(.18f,.13f,.24f),false,false,false,true);
        Room("Vault",new Vector2(13,0),new Color(.22f,.18f,.10f),false,false,true,false);
        Corridor(new Vector2(0,5.5f),true);Corridor(new Vector2(0,-5.5f),true);
        Corridor(new Vector2(-6.5f,0),false);Corridor(new Vector2(6.5f,0),false);
        Prefab("B","SafeHouse",Vector2.zero,world);Prefab("B","VaultDoor",new Vector2(6.5f,0),world);
        Sign("SAFE HOUSE",new Vector2(0,.85f),new Color(.45f,.94f,.72f),3.8f);
        Sign("AUTO BANK",new Vector2(0,-.9f),new Color(.36f,.73f,.59f),3.8f);
        Sign("NORTH / STORAGE",new Vector2(0,14.3f),new Color(.79f,.75f,.49f));
        Sign("SOUTH / ARCHIVE",new Vector2(0,-7.2f),new Color(.46f,.72f,.9f));
        Sign("WEST / COLLECTION",new Vector2(-13,3.8f),new Color(.74f,.57f,.94f));
        Sign("VAULT / KEY REQUIRED",new Vector2(13,3.8f),new Color(.94f,.76f,.35f));
        Sign("NORTH",new Vector2(0,3.8f),new Color(.63f,.69f,.7f),3);
        Sign("SOUTH",new Vector2(0,-3.8f),new Color(.63f,.69f,.7f),3);
        Sign("WEST",new Vector2(-4.2f,.95f),new Color(.74f,.57f,.94f),2.8f);
        Sign("VAULT",new Vector2(4.2f,.95f),new Color(.94f,.76f,.35f),2.8f);
        Prefab("B","Trap",new Vector2(-1.2f,9),world);
        Prefab("B","Trap",new Vector2(1.3f,-10),world);
        Prefab("B","Trap",new Vector2(-11.5f,-1.2f),world);
        Prefab("B","Trap",new Vector2(11.2f,-.9f),world);
        Prefab("A","Player",Vector2.zero);
        Prefab("A","RoundManager",Vector2.zero);Prefab("A","HUD",Vector2.zero);Prefab("A","ItemFactory",Vector2.zero);Prefab("B","RoundScreens",Vector2.zero);
        var cam=Group("Main Camera").gameObject;cam.tag="MainCamera";cam.transform.position=new Vector3(0,0,-10);
        var camera=cam.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.5f;camera.backgroundColor=new Color(.025f,.04f,.065f);camera.clearFlags=CameraClearFlags.SolidColor;
        cam.AddComponent<AudioListener>();cam.AddComponent<CameraFollow>();
        var light=Group("Global Light 2D").gameObject.AddComponent<Light2D>();light.lightType=Light2D.LightType.Global;light.intensity=1;
        Vector2[] seven={new Vector2(-4,2.5f),new Vector2(0,2.5f),new Vector2(4,2.5f),new Vector2(-4,0),new Vector2(4,0),new Vector2(-3,-3),new Vector2(3,-3)};
        Spawner("Hall Items",Vector2.zero,new[]{"Low"},new[]{1f},new[]{new Vector2(-4,2),new Vector2(4,2),new Vector2(-4,-2),new Vector2(4,-2)});
        Spawner("North Items",new Vector2(0,11),new[]{"Low","Mid","High"},new[]{3f,3f,1f},seven);
        Spawner("South Items",new Vector2(0,-11),new[]{"Mid","High"},new[]{3f,2f},seven);
        Spawner("West Items",new Vector2(-13,0),new[]{"High","Gem"},new[]{2f,1.5f},seven);
        Spawner("Vault Items",new Vector2(13,0),new[]{"High","Gem"},new[]{1f,1f},new[]{new Vector2(-3,2),new Vector2(3,2),new Vector2(-3,-2),new Vector2(3,-2)});
        var keys=Prefab("A","KeySpawner",Vector2.zero);
        while(keys.transform.childCount>0)UnityEngine.Object.DestroyImmediate(keys.transform.GetChild(0).gameObject);
        foreach(var pos in new[]{new Vector2(-3,14),new Vector2(3,-14),new Vector2(-16,1.5f)}){var p=Group("KeyCandidate",keys.transform);p.position=pos;}
        if(test) Sign("B INTEGRATION TEST",new Vector2(0,-2.6f),new Color(.5f,.6f,.65f),6);
        EditorSceneManager.SaveScene(scene,path);
    }

    [MenuItem("Tools/The Collector/Build WebGL")]
    public static void BuildWeb()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode before building.");
        if(!File.Exists(Main))throw new Exception("Main scene missing.");
        Directory.CreateDirectory("docs");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{Main}, locationPathName="docs", target=BuildTarget.WebGL, options=BuildOptions.None });
        // Temp is recreated by Unity; menu builds may run without automation enabled.
        Directory.CreateDirectory(Control);
        File.WriteAllText(Path.Combine(Control,"build.txt"),report.summary.result+"\nbytes="+report.summary.totalSize+"\nseconds="+report.summary.totalTime.TotalSeconds+"\nerrors="+report.summary.totalErrors);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        File.WriteAllText("docs/.nojekyll","");
    }
}
#endif
