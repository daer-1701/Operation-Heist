using System;
using System.IO;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperationHeist.Editor
{
    public static class LocalMvpBuilder
    {
        public const string ScenePath = "Assets/00_Scenes/Heist_Depot.unity";
        private static Material floor, wall, dark, teal, amber, white;

        [MenuItem("Operation Heist/MVP/Añadir salud y objetivos al depósito abierto")]
        public static void PatchSystemsOntoOpenDepot()
        {
            var mission = UnityEngine.Object.FindFirstObjectByType<LocalMission>();
            var player = UnityEngine.Object.FindFirstObjectByType<FpsController>();
            if (mission == null || player == null)
            {
                EditorUtility.DisplayDialog("Sistemas", "Abre Heist_Depot con LocalMission y jugador.", "OK");
                return;
            }
            var health = player.GetComponent<PlayerHealth>() ?? player.gameObject.AddComponent<PlayerHealth>();
            health.Configure(100f, 12f, false);
            if (player.GetComponent<HealthHudPresenter>() == null)
                player.gameObject.AddComponent<HealthHudPresenter>().Configure(health);
            var board = mission.GetComponent<ObjectiveBoard>() ?? mission.gameObject.AddComponent<ObjectiveBoard>();
            board.Configure(new[]
            {
                new MissionObjectiveState("deposit-1", "Asegurar expediente 1"),
                new MissionObjectiveState("deposit-2", "Asegurar expediente 2"),
                new MissionObjectiveState("deposit-3", "Asegurar expediente 3"),
                new MissionObjectiveState("loot-secured", "Botín completo en extracción"),
                new MissionObjectiveState("extract", "Evacuar del depósito")
            });
            mission.ConfigureSystems(health, board);
            EditorSceneManager.MarkSceneDirty(mission.gameObject.scene);
            Debug.Log("LOCAL_MVP_SYSTEMS_PATCHED");
        }

        [MenuItem("Operation Heist/MVP/Generar deposito (reemplaza escena generada)")]
        public static void Build()
        {
            if (!Application.isBatchMode && File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Regenerar depósito", "Se reemplazará Heist_Depot. Conserva una copia si hiciste cambios manuales.", "Regenerar", "Cancelar")) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            floor = Mat("OH_DepotFloor", new Color(.12f,.16f,.19f));
            wall = Mat("OH_DepotWall", new Color(.3f,.37f,.4f));
            dark = Mat("OH_DepotDark", new Color(.045f,.07f,.09f));
            teal = Mat("OH_DepotTeal", new Color(.08f,.65f,.6f));
            amber = Mat("OH_DepotAmber", new Color(1f,.58f,.12f));
            white = Mat("OH_DepotWhite", new Color(.8f,.88f,.87f));
            Box("Suelo",new Vector3(0,-.3f,0),new Vector3(28,.6f,32),floor);
            Box("Pared norte",new Vector3(0,2,16),new Vector3(28,4,.4f),wall);
            Box("Pared sur",new Vector3(0,2,-16),new Vector3(28,4,.4f),wall);
            Box("Pared oeste",new Vector3(-14,2,0),new Vector3(.4f,4,32),wall);
            Box("Pared este",new Vector3(14,2,0),new Vector3(.4f,4,32),wall);
            for(int x=-12;x<=12;x+=4)
            {
                Box("Guia de suelo",new Vector3(x,.008f,0),new Vector3(.035f,.01f,31),wall,false);
                Box("Viga",new Vector3(x,3.9f,0),new Vector3(.16f,.16f,32),dark,false);
            }
            for(int z=-12;z<=12;z+=4)
                Box("Junta",new Vector3(0,.009f,z),new Vector3(27,.01f,.035f),wall,false);
            Box("Techo",new Vector3(0,4.05f,0),new Vector3(28,.2f,32),dark,false);
            Box("Zona de extraccion",new Vector3(0,.012f,-12),new Vector3(7,.02f,5),teal,false);
            Box("Marco puerta A",new Vector3(-2.2f,1.6f,-15.7f),new Vector3(.35f,3.2f,.35f),dark);
            Box("Marco puerta B",new Vector3(2.2f,1.6f,-15.7f),new Vector3(.35f,3.2f,.35f),dark);
            Box("Dintel",new Vector3(0,3.15f,-15.7f),new Vector3(4.8f,.35f,.35f),dark);
            Box("Mostrador despacho",new Vector3(0,.55f,-14),new Vector3(4,1.1f,1),dark);
            Prop("ServiceCounter",new Vector3(-8,0,-13.5f),Quaternion.Euler(0,0,0));
            Prop("CompactSafe",new Vector3(9,0,-13.2f),Quaternion.identity);
            var terminal=Box("Terminal de extraccion",new Vector3(0,1.35f,-13.9f),new Vector3(.9f,.55f,.4f),teal);
            Box("Pantalla terminal",new Vector3(0,1.55f,-13.65f),new Vector3(.7f,.28f,.05f),white,false);
            // Broad partitions form three storage aisles with entrances at both ends.
            foreach(float x in new[]{-5f,5f})
            {
                Box("Separador de almacen",new Vector3(x,.8f,5),new Vector3(.35f,1.6f,9),wall);
                Box("Banda de seguridad",new Vector3(x,1.62f,5),new Vector3(.38f,.08f,9),amber,false);
                Box("Columna",new Vector3(x,2f,-2),new Vector3(.55f,4,.55f),dark);
                Box("Columna",new Vector3(x,2f,12),new Vector3(.55f,4,.55f),dark);
            }
            foreach(float x in new[]{-10f,10f})
            {
                foreach(float z in new[]{-3f,3f,9f})
                {
                    Prop("StorageShelf",new Vector3(x,0,z),Quaternion.Euler(0,90,0));
                    Box("Carga almacenada",new Vector3(x+Mathf.Sign(x)*.55f,.5f,z+1.2f),new Vector3(1.1f,1,1.1f),dark);
                }
            }
            Box("Oficina lateral",new Vector3(-11.5f,1.2f,-8),new Vector3(4,2.4f,5),wall);
            Box("Cabina vigilancia",new Vector3(11.2f,1.1f,-7.5f),new Vector3(3.5f,2.2f,4),dark);
            Box("Ventana cabina",new Vector3(9.6f,1.4f,-7.5f),new Vector3(.08f,1.1f,2.4f),teal,false);
            Box("Cobertura de entrada",new Vector3(-2,.65f,-4),new Vector3(3,1.3f,1.2f),dark);
            Box("Cobertura de entrada",new Vector3(4,.65f,-4),new Vector3(2,1.3f,1.2f),dark);
            Box("Palet",new Vector3(-6,.12f,-1),new Vector3(1.6f,.24f,1.2f),amber);
            Box("Palet",new Vector3(6.5f,.12f,2),new Vector3(1.6f,.24f,1.2f),amber);

            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Player/FPSPlayer.prefab");
            var player=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
            player.name="Agente local"; player.transform.position=new Vector3(0,.05f,-10);
            var motor=player.GetComponent<FpsController>();
            var carrier=player.AddComponent<LootCarrier>();
            var health=player.GetComponent<PlayerHealth>() ?? player.AddComponent<PlayerHealth>();
            health.Configure(100f, 12f, false);
            player.AddComponent<HealthHudPresenter>().Configure(health);
            var mission=new GameObject("Mision - Ultimo turno").AddComponent<LocalMission>();
            var board=mission.gameObject.AddComponent<ObjectiveBoard>();
            board.Configure(new[]
            {
                new MissionObjectiveState("deposit-1", "Asegurar expediente 1"),
                new MissionObjectiveState("deposit-2", "Asegurar expediente 2"),
                new MissionObjectiveState("deposit-3", "Asegurar expediente 3"),
                new MissionObjectiveState("loot-secured", "Botín completo en extracción"),
                new MissionObjectiveState("extract", "Evacuar del depósito")
            });
            mission.Configure(motor,carrier,300,3);
            mission.ConfigureSystems(health, board);
            carrier.Configure(mission);
            mission.gameObject.AddComponent<MissionAudio>().Configure(mission);
            terminal.AddComponent<ExtractionPoint>().Configure(mission);
            var presenter=player.AddComponent<LocalMissionPresenter>(); presenter.Configure(mission);
            Loot("DOSSIER / 01",new Vector3(-8,.05f,7));
            Loot("RESERVA / 02",new Vector3(8,.05f,8));
            Loot("ARCHIVO / 03",new Vector3(0,.05f,13));
            PointLight("Luz objetivo 01",new Vector3(-8,3.2f,7),amber.color,.9f,8f);
            PointLight("Luz objetivo 02",new Vector3(8,3.2f,8),amber.color,.9f,8f);
            PointLight("Luz objetivo 03",new Vector3(0,3.2f,13),amber.color,.9f,8f);
            PointLight("Luz extraccion",new Vector3(0,3.4f,-12),teal.color,1.1f,10f);

            var guardRoot=new GameObject("Guardia de seguridad"); guardRoot.transform.position=new Vector3(-2.8f,0,1);
            Part(guardRoot.transform,"Torso",new Vector3(0,1.05f,0),new Vector3(.55f,.65f,.35f),amber);
            Part(guardRoot.transform,"Cabeza",new Vector3(0,1.6f,0),new Vector3(.32f,.35f,.32f),white);
            Part(guardRoot.transform,"Visor",new Vector3(0,1.65f,.18f),new Vector3(.3f,.12f,.06f),dark);
            foreach(float x in new[]{-.17f,.17f}) Part(guardRoot.transform,"Pierna",new Vector3(x,.38f,0),new Vector3(.2f,.75f,.25f),dark);
            var guardCollider=guardRoot.AddComponent<CapsuleCollider>(); guardCollider.center=new Vector3(0,.9f,0); guardCollider.height=1.8f; guardCollider.radius=.3f;
            var routeRoot=new GameObject("Ruta de patrulla");
            var points=new Transform[4];
            var positions=new[]{new Vector3(-2.8f,0,1),new Vector3(2.8f,0,1),new Vector3(2.8f,0,10),new Vector3(-2.8f,0,10)};
            for(int i=0;i<points.Length;i++){points[i]=new GameObject("Punto "+i).transform; points[i].SetParent(routeRoot.transform); points[i].position=positions[i];}
            guardRoot.AddComponent<PatrolGuard>().Configure(mission,points);
            Sign("EXTRACCION",new Vector3(0,2.8f,-15.7f),Quaternion.Euler(0,180,0),teal.color,.13f);
            Sign("DEPOSITO 07 / ULTIMO TURNO",new Vector3(0,3,15.7f),Quaternion.identity,white.color,.12f);
            Sign("01",new Vector3(-8,2.5f,12),Quaternion.identity,amber.color,.25f);
            Sign("02",new Vector3(8,2.5f,12),Quaternion.identity,amber.color,.25f);
            Sign("03",new Vector3(0,2.3f,15.65f),Quaternion.identity,amber.color,.25f);
            var sun=new GameObject("Luz principal").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.7f;
            sun.transform.rotation=Quaternion.Euler(55,-25,0); sun.shadows=LightShadows.Soft;
            RenderSettings.skybox=null; RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.45f,.52f,.58f);
            motor.PlayerCamera.backgroundColor=new Color(.055f,.085f,.11f);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/00_Scenes/FPSPrototype.unity",true)};
            AssetDatabase.SaveAssets(); Debug.Log("LOCAL_MVP_GENERATED");
        }

        public static void BuildWindows()
        {
            string output=Environment.GetEnvironmentVariable("OH_BUILD_OUTPUT");
            if(string.IsNullOrEmpty(output)) output=Path.GetFullPath("Builds/OperationHeist/OperationHeist.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build fallido: "+report.summary.result);
            Debug.Log("LOCAL_MVP_BUILD_OK: "+output);
        }

        private static Material Mat(string name,Color color)=>FpsPrototypeBuilder.OriginalMaterial(name,color);
        private static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,bool collider=true)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.position=position; go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=FpsPrototypeBuilder.OriginalCuboid(); go.GetComponent<MeshRenderer>().sharedMaterial=material;
            if(collider)go.AddComponent<BoxCollider>(); return go;
        }
        private static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {var p=Box(name,Vector3.zero,scale,material,false);p.transform.SetParent(parent,false);p.transform.localPosition=position;}
        private static GameObject Prop(string name,Vector3 position,Quaternion rotation)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Props/OH_"+name+".prefab");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.SetPositionAndRotation(position,rotation);
            var inspect=instance.GetComponent<PrototypeInteractable>();if(inspect!=null)UnityEngine.Object.DestroyImmediate(inspect);
            return instance;
        }
        private static void Loot(string label,Vector3 position)
        {
            var prop=Prop("TransportCrate",position,Quaternion.identity);prop.name=label; prop.AddComponent<LootItem>().Configure(label);
            Box("Marca de objetivo",position+Vector3.up*.015f,new Vector3(1.4f,.02f,1.4f),amber,false);
        }
        private static void Sign(string value,Vector3 position,Quaternion rotation,Color color,float size)
        {
            var go=new GameObject(value);go.transform.SetPositionAndRotation(position,rotation);
            var text=go.AddComponent<TextMesh>(); text.text=value;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.characterSize=size;text.fontSize=64;text.color=color;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
        private static void PointLight(string name,Vector3 position,Color color,float intensity,float range)
        {
            var light=new GameObject(name).AddComponent<Light>();
            light.type=LightType.Point; light.color=color; light.intensity=intensity; light.range=range;
            light.shadows=LightShadows.None; light.transform.position=position;
        }
    }
}
