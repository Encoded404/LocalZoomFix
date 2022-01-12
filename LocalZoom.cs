using System.Collections;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LocalZoom
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class LocalZoom : BaseUnityPlugin
    {
        private const string ModId = "com.bosssloth.rounds.LocalZoom";
        private const string ModName = "LocalZoom";
        public const string Version = "1.0.0";

        public static LocalZoom instance;

        public static GameObject border;

        public static float defaultMapSize = 20f;
        
        public bool enableCamera = true;

        public static AssetBundle shaderBundle;

        public Harmony harmony;

        // Sources:
        // Sprite stencil shader (Modified) https://prime31.github.io/stencil-buffer-occlusion/
        // Sprites default shader https://github.com/nubick/unity-utils/blob/master/sources/Assets/Scripts/Shaders/Sprites-Default.shader
        // https://answers.unity.com/questions/590800/how-to-cullrender-to-through-a-window.html
        // https://www.youtube.com/watch?v=CSeUMTaNFYk

        //TODO: 
        // - Patch the outofbounds bounce handler so it doesn't bounce of the camera bounds
        // - Patch the cardbar preview card to be screen space instead of world space
        
        //TODO IMPORTANT:
        // Somehow figure out why the stencil is showing the particles and not just nothing


        private void Start()
        {
            instance = this;
            
            harmony = new Harmony(ModId);
            harmony.PatchAll();

            // Unbound.RegisterClientSideMod(ModId);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, RoundStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, RoundEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, StartPickPhase);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd,EndPickPhase);

            try
            {
                shaderBundle = AssetUtils.LoadAssetBundleFromResources("localcam", typeof(LocalZoom).Assembly);
                if (shaderBundle == null)
                {
                    UnityEngine.Debug.LogError("Couldn't find shaderBundle?");
                }
            }
            catch
            {
                // ignored
            }
        }

        void Update()
        {
            // Unbound.lockInputBools["chatLock"] = isLockingInput;
            
            // If not in sandbox and in offlinemode return
            if (!(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode)
                return;


            if(GameManager.instance.battleOngoing && !CardChoice.instance.IsPicking) {
                if (enableCamera)
                {
                    var player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);

                    if (player == null || !player.data.isPlaying) return;

                    var gunTransform = player.data.weaponHandler.gun.transform.GetChild(0);
                    // SetCameraPosition(gunTransform.position + gunTransform.forward*1.5f);
                    SetCameraPosition(player.transform.position);
                    
                    try
                    {
                        border.transform.position = Vector3.zero;
                    }
                    // If border is null we set it up
                    catch
                    {
                        #if DEBUG
                        if (SceneManager.GetSceneAt(0).GetRootGameObjects()
                                .FirstOrDefault(o => o.name == "BorderCanvas") != null)
                        {
                            border = SceneManager.GetSceneAt(0).GetRootGameObjects()
                                .FirstOrDefault(o => o.name == "BorderCanvas")
                                ?.transform.GetChild(0).gameObject;
                        }
                        else
                        {
                        #endif
                            MapManager.instance.currentMap.Map.size = defaultMapSize;
                            border = UIHandler.instance.transform.Find("Canvas/Border").gameObject;
                            var canvas = new GameObject("BorderCanvas").AddComponent<Canvas>();
                            border.transform.SetParent(canvas.transform);
                            border.transform.position = Vector3.zero;
                            canvas.renderMode = RenderMode.WorldSpace;
#if DEBUG
                        }
                        #endif
                    }

                    if (Input.mouseScrollDelta.y > 0)
                    {
                        // zoom in
                        MapManager.instance.currentMap.Map.size -= 1f;
                    } else if (Input.mouseScrollDelta.y < 0)
                    {
                        // zoom out
                        MapManager.instance.currentMap.Map.size += 1f;
                    }
                }
            }
            else
            {
                SetCameraPosition(Vector3.zero);
                if (!CardChoice.instance.IsPicking)
                {
                    if (MapManager.instance.currentLevelID != 0)
                    {
                        MapManager.instance.currentMap.Map.size = defaultMapSize;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                var obj = new GameObject("ViewSphere");
                obj.AddComponent<MeshFilter>();
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.material = new Material(shaderBundle.LoadAsset<Shader>("CustomPortal"));
                var player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);
                if (player == null) return;
                obj.AddComponent<ViewSphere>().player = player;
                obj.transform.SetParent(player.transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.SetZPosition(5);
                obj.transform.localRotation = Quaternion.identity;

                var black = Instantiate(shaderBundle.LoadAsset<GameObject>("BlackBox"), player.transform);
                black.transform.localPosition = Vector3.zero;
                var circle = Instantiate(shaderBundle.LoadAsset<GameObject>("PlayerCircle"), player.transform);
                circle.transform.localPosition = Vector3.zero;
                circle.transform.localScale = Vector3.one * 2.65f;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomHidden"));
                foreach (var player in PlayerManager.instance.players)
                {
                    MakeObjectHidden(player);
                    
                    foreach (var renderer in player.data.weaponHandler.gun.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (renderer.material.name.Contains("Default"))
                        {
                            renderer.material = mat;
                        }
                        else
                        {
                            renderer.material.shader = mat.shader;
                        }
                    }
                    foreach (var img in player.data.weaponHandler.gun.GetComponentsInChildren<Image>(true))
                    {
                        img.material = mat;
                    }
                }
            }

            // if (Input.GetKeyDown(KeyCode.K))
            // {
            //     var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomHidden"));
            //     foreach (var player in PlayerManager.instance.players)
            //     {
            //         foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
            //         {
            //             if (renderer.material.name.Contains("Default"))
            //             {
            //                 renderer.material = mat;
            //             }
            //             else
            //             {
            //                 renderer.material.shader = mat.shader;
            //             }
            //             // var material = renderer.material;
            //             // var col = material.color;
            //             // var tex = material.mainTexture;
            //             //
            //             // renderer.sharedMaterial = mat;
            //             // renderer.sharedMaterial.color = col;
            //             // renderer.sharedMaterial.mainTexture = tex;
            //         }
            //         foreach (var img in player.GetComponentsInChildren<Image>(true))
            //         {
            //             img.material = mat;
            //         }
            //         
            //         foreach (var renderer in player.data.weaponHandler.gun.GetComponentsInChildren<SpriteRenderer>(true))
            //         {
            //             if (renderer.material.name.Contains("Default"))
            //             {
            //                 renderer.material = mat;
            //             }
            //             else
            //             {
            //                 renderer.material.shader = mat.shader;
            //             }
            //         }
            //         foreach (var img in player.data.weaponHandler.gun.GetComponentsInChildren<Image>(true))
            //         {
            //             img.material = mat;
            //         }
            //         
            //         
            //         foreach (var ren in player.GetComponentsInChildren<ParticleSystemRenderer>(true))
            //         {
            //             ren.material.shader = mat.shader;
            //         }
            //     }
            //
            //     var test = Instantiate(shaderBundle.LoadAsset<GameObject>("New Sprite"));
            //     test.transform.position = Vector3.zero;
            //     test.transform.SetZPosition(5);
            //     test.transform.localScale = new Vector3(13, 13, 13);
            //     test.GetComponent<SpriteRenderer>().material.shader = shaderBundle.LoadAsset<Shader>("CustomPortal");
            //
            //     // var test = new GameObject("test").AddComponent<SpriteRenderer>();
            //     // test.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            //     // test.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            //     // test.color = Color.black*0.5f;
            //
            // }
        }

        [HarmonyPatch(typeof(Map),"Start")]
        class MapPatchStart
        {
            private static void Postfix(Map __instance)
            {
                defaultMapSize = __instance.size;
            }
        }

        IEnumerator RoundEnd(IGameModeHandler gm)
        {
            SetCameraPosition(Vector3.zero);
            MapManager.instance.currentMap.Map.size = defaultMapSize;
            yield break;
        }
        
        IEnumerator RoundStart(IGameModeHandler gm)
        {
            // SetCameraPosition(Vector3.zero, true);
            this.ExecuteAfterSeconds(1f, () =>
            {
                MapManager.instance.currentMap.Map.size = defaultMapSize/2f;
            });
            yield break;
        }

        IEnumerator StartPickPhase(IGameModeHandler gm)
        {
            enableCamera = false;
            yield break;
        }
        IEnumerator EndPickPhase(IGameModeHandler gm)
        {
            StartCoroutine(DisableCameraTemp());
            yield break;
        }

        IEnumerator DisableCameraTemp()
        {
            enableCamera = false;
            yield return new WaitForSeconds(1);
            enableCamera = true;
        }

        public void SetCameraPosition(Vector3 pos, bool snap = false)
        {
            var mainCam = MainCam.instance.gameObject.transform;
            mainCam.position = snap? pos : Vector3.Lerp(mainCam.position, pos, Time.deltaTime * 5);
            mainCam.SetZPosition(-100);
            var lightCam = MainCam.instance.transform.parent.GetChild(1).GetChild(0);
            lightCam.position = snap ? pos : Vector3.Lerp(lightCam.position, pos, Time.deltaTime * 5);
            lightCam.SetZPosition(-100);
            // if (snap)
            // {
            //     lightCam.transform.GetChild(0).localPosition = pos;
            //     lightCam.transform.GetChild(0).SetZPosition(-100);
            // }
        }

        public static void MakeObjectHidden(Component obj)
        {
            var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomHidden"));
            foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }
            foreach (var img in obj.GetComponentsInChildren<Image>(true))
            {
                img.material = mat;
            }
            foreach (var renderer in obj.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }
            foreach (var renderer in obj.GetComponentsInChildren<TrailRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }
        }

        private static void ChangeMaterial(Renderer renderer, Material mat)
        {
            var mats = renderer.materials;
            for (var i = 0; i < renderer.materials.Length; i++)
            {
                if (renderer.materials[i].name.Contains("Default"))
                {
                    mats[i] = mat;
                }
                else
                {
                    //TODO: Block circle becomes triangle, why?
                    UnityEngine.Debug.Log("------");
                    // var colorCopy = mats[i].color;
                    if (mats[i].mainTexture != null)
                    {
                        UnityEngine.Debug.Log(mats[i].mainTexture.name);
                    }
                    var textureCopy = mats[i].mainTexture;
                    mats[i] = mat;
                    // mats[i].color = colorCopy;
                    if(mats[i].mainTexture != null && mats[i].mainTexture.name.Contains("Triangle2"))
                    {
                        UnityEngine.Debug.Log("WHy?");
                    }
                    mats[i].mainTexture = textureCopy;
                    if (mats[i].mainTexture != null)
                    {
                        UnityEngine.Debug.Log(mats[i].mainTexture.name);
                    }
                }
            }
            renderer.materials = mats;
        }

        private void OnDestroy()
        {
            GameModeManager.RemoveHook(GameModeHooks.HookRoundStart, RoundStart);
            GameModeManager.RemoveHook(GameModeHooks.HookRoundEnd, RoundEnd);
            GameModeManager.RemoveHook(GameModeHooks.HookPickStart, StartPickPhase);
            GameModeManager.RemoveHook(GameModeHooks.HookPickEnd,EndPickPhase);
            harmony.UnpatchAll();
        }
    }
}