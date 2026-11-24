#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
internal static class TestBombeRomainTemp
{
    const string Report="Temp/TestBombeRomainV7.txt", Key="TestBombeRomainV7";
    const string Game="Assets/Scenes/GameScene/DecoupeTissu.unity", Test="Assets/Scenes/SceneRomain_Test/SampleScene.unity";
    static int phase,index;
    static double since;
    static SprayInput[] cans;
    static PaintableSurface surface;
    static XRDirectInteractor hand;
    static bool game;
    static int maxParticles;
    static Vector3 point,normal;
    static void Log(string s)=>File.AppendAllText(Report,s+"\n");
    static TestBombeRomainTemp(){ EditorApplication.update+=Tick; }
    static void Tick(){if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        try{Run();}catch(Exception e){End(e.ToString());}}
    static void End(string s){Log(s);EditorApplication.update-=Tick;bool own=SessionState.GetBool(Key,false);SessionState.SetBool(Key,false);if(own && EditorApplication.isPlaying)EditorApplication.ExitPlaymode();}
    static void Run(){
        if(!SessionState.GetBool(Key,false)){
            if(File.Exists(Report)){EditorApplication.update-=Tick;return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode){End("Editor busy");return;}
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/BombePeinture.prefab");
            var barrel=model.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="MSH_canon");
            var verts=barrel.sharedMesh.vertices.Select(v=>model.transform.InverseTransformPoint(barrel.transform.TransformPoint(v))).ToArray();
            float front=verts.Max(v=>v.z);var rim=verts.Where(v=>v.z>front-0.001f).ToArray();
            Log("Barrel rim center: "+((rim.Aggregate(Vector3.zero,(a,b)=>a+b))/rim.Length).ToString("F5"));
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;
        }
        if(!EditorApplication.isPlaying || Time.frameCount<5)return;
        var scene=SceneManager.GetActiveScene();
        double now=EditorApplication.timeSinceStartup;
        if(phase==0){
            string path=game?Game:Test;
            if(scene.path!=path){EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));return;}
            var roots=scene.GetRootGameObjects();
            cans=roots.SelectMany(o=>o.GetComponentsInChildren<SprayInput>(true)).ToArray();
            if(cans.Length!=(game?3:1))throw new Exception("Cans: "+cans.Length);
            if(game){
                roots.Single(o=>o.name=="XR Origin (XR Rig)").transform.position-=Vector3.forward;
                var kite=roots.Single(o=>o.name=="CerfVolant Papillon");kite.SetActive(true);
                surface=kite.GetComponentInChildren<PaintableSurface>(true);
                var patron=kite.GetComponentInChildren<PatronPointilles>(true);
                ((UnityEngine.Events.UnityEvent)typeof(PatronPointilles).GetField("quandToutEstRelie",
                    System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(patron)).Invoke();
            }else surface=roots.SelectMany(o=>o.GetComponentsInChildren<PaintableSurface>(true)).Single();
            hand=new GameObject("Test hand").AddComponent<XRDirectInteractor>();
            since=now;phase=1;return;
        }
        if(phase==1){
            if(now-since<2.0)return;
            var can=cans[index];can.gameObject.SetActive(true);
            var mesh=surface.GetComponent<MeshCollider>().sharedMesh;
            var tri=mesh.triangles;var v=mesh.vertices;
            point=surface.transform.TransformPoint((v[tri[0]]+v[tri[1]]+v[tri[2]])/3);
            normal=surface.transform.TransformDirection(Vector3.Cross(v[tri[1]]-v[tri[0]],v[tri[2]]-v[tri[0]]).normalized);
            var grab=can.GetComponent<XRGrabInteractable>();hand.StartManualInteraction((IXRSelectInteractable)grab);
            grab.trackPosition=false;grab.trackRotation=false;
            can.GetComponent<Rigidbody>().isKinematic=true;
            can.transform.rotation=Quaternion.LookRotation(-normal);
            can.transform.position+=point+normal*.55f-can.GetComponent<SprayConeVFX>().SprayOrigin.position;
            Physics.SyncTransforms();
            if(Physics.Raycast(can.GetComponent<SprayConeVFX>().SprayOrigin.position,-normal,out var hit,1.5f))
                Log("First hit "+hit.collider.name+"; target "+surface.name);
            grab.activated.Invoke(new ActivateEventArgs{interactorObject=hand,interactableObject=grab});
            maxParticles=0;since=now;phase=2;return;
        }
        var aiming=cans[index];
        aiming.transform.rotation=Quaternion.LookRotation(-normal);
        aiming.transform.position+=point+normal*.35f-aiming.GetComponent<SprayConeVFX>().SprayOrigin.position;
        Physics.SyncTransforms();
        var emitter=aiming.GetComponent<SprayConeVFX>().SprayOrigin;
        maxParticles=Mathf.Max(maxParticles,emitter.GetComponent<ParticleSystem>().particleCount);
        if(now-since<1.2)return;
        var tool=cans[index];var col=tool.GetComponent<SprayColor>().PaintColor;
        var renderer=tool.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="MSH_reservoir");
        var props=new MaterialPropertyBlock();renderer.GetPropertyBlock(props);
        if(props.GetColor("_BaseColor")!=col)throw new Exception("Reservoir color mismatch");
        var tex=(Texture2D)surface.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BaseMap");
        var nearest=tex.GetPixels().Min(p=>Mathf.Abs(p.r-col.r)+Mathf.Abs(p.g-col.g)+Mathf.Abs(p.b-col.b));
        var zones=new SerializedObject(tool.GetComponent<SprayPainter>()).FindProperty("paintableSurfaces");
        Log("Color="+col+" nearest="+nearest+" zones="+zones.arraySize+" allowed="+Enumerable.Range(0,zones.arraySize).Any(i=>zones.GetArrayElementAtIndex(i).objectReferenceValue==surface)+" trigger="+tool.IsSpraying+" coverage="+new SerializedObject(surface).FindProperty("progressionPourcent").floatValue);
        if(nearest>0.25f)throw new Exception("Paint missing: "+tool.name);
        var ps=tool.GetComponent<SprayConeVFX>().SprayOrigin.GetComponent<ParticleSystem>();
        Log("Held="+tool.IsSpraying+" emitting="+ps.isEmitting+" count="+ps.particleCount);
        Log("peak="+maxParticles+" normal="+normal+" blockers="+string.Join(",",Physics.OverlapSphere(emitter.position,0.015f).Select(c=>c.name)));
        if(!tool.IsSpraying || !ps.isEmitting || maxParticles<5)throw new Exception("No spray");
        Log(scene.name+" : "+tool.name+" painted "+col+" particles="+tool.GetComponentInChildren<ParticleSystem>().particleCount);
        hand.EndManualInteraction();
        if(++index<cans.Length){phase=1;since=now;return;}
        if(!game){game=true;phase=index=0;return;}
        End("PASS");
    }
}
#endif
