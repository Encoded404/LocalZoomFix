using System.Collections;
using System.Linq;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Utils;
using Photon.Pun;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace LocalZoom
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInDependency("pykess.rounds.plugins.mapembiggener")]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class LocalZoom : BaseUnityPlugin
    {
        private const string ModId = "com.bosssloth.rounds.LocalZoom";
        private const string ModName = "LocalZoom";
        public const string Version = "1.0.0";

        public static LocalZoom instance;

        public static float defaultMapSize = 20f;
        
        public bool enableCamera = true;
        public bool enableResetCamera = false;
        public GameObject deathPortalBox;

        private static AssetBundle shaderBundle;

        private Harmony harmony;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        // Sources:
        // Sprite stencil shader (Modified) https://prime31.github.io/stencil-buffer-occlusion/
        // Sprites default shader https://github.com/nubick/unity-utils/blob/master/sources/Assets/Scripts/Shaders/Sprites-Default.shader
        // https://answers.unity.com/questions/590800/how-to-cullrender-to-through-a-window.html
        // https://www.youtube.com/watch?v=CSeUMTaNFYk

        //TODO: 
        // spectator camera when dead?
        // BUG weird color change of the jump particle
        
        //TODO WEIRD:
        // Somehow figure out why the stencil is showing the particles and not just nothing
        // looks like this is some bug with a sprite mask, all particles that are hidden by a sprite mask
        // will always show up in any stencil buffer.


        private void Awake()
        {
            On.MainMenuHandler.Awake += (orig, self) =>
            {
                enableResetCamera = false;
                enableCamera = true;
                orig(self);
            };
        }

        private void Start()
        {
            instance = this;
            
            harmony = new Harmony(ModId);
            harmony.PatchAll();

            // Unbound.RegisterClientSideMod(ModId);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, Numerators.RoundStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, Numerators.RoundEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, Numerators.StartPickPhase);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd,Numerators.EndPickPhase);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, Numerators.GameStarted);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, Numerators.PointStart);

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


            if(GameManager.instance.battleOngoing && !CardChoice.instance.IsPicking && !enableResetCamera) {
                if (enableCamera)
                {
                    var player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);

                    if (player == null || !player.data.isPlaying) return;

                    var gunTransform = player.data.weaponHandler.gun.transform.GetChild(0);
                    // SetCameraPosition(gunTransform.position + gunTransform.forward*1.5f);
                    SetCameraPosition(player.transform.position);

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

            if (!CardChoice.instance.IsPicking && enableResetCamera)
            {
                SetCameraPosition(Vector3.zero);
                if (MapManager.instance.currentLevelID != 0)
                {
                    MapManager.instance.currentMap.Map.size = defaultMapSize;
                }

                if (deathPortalBox != null)
                {
                    deathPortalBox.SetActive(true);
                }
            }
            else
            {
                if (deathPortalBox != null)
                {
                    deathPortalBox.SetActive(false);
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
                obj.transform.SetZPosition(50);
                obj.transform.localRotation = Quaternion.identity;

                var black = Instantiate(shaderBundle.LoadAsset<GameObject>("BlackBox"), player.transform);
                black.transform.localPosition = Vector3.zero;
                black.transform.localScale = Vector3.one * 100f;
                var circle = Instantiate(shaderBundle.LoadAsset<GameObject>("PlayerCircle"), player.transform);
                circle.transform.localPosition = Vector3.zero;
                circle.transform.localScale = Vector3.one * 2.7f;
                
                deathPortalBox = Instantiate(shaderBundle.LoadAsset<GameObject>("BlackBox"));
                deathPortalBox.transform.position = Vector3.zero;
                deathPortalBox.transform.localScale = Vector3.one * 100f;
                deathPortalBox.GetComponent<Renderer>().material = new Material(shaderBundle.LoadAsset<Shader>("CustomPortal"));
                deathPortalBox.transform.SetZPosition(50);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                var shad = shaderBundle.LoadAsset<Shader>("CustomHidden");
                var mat = new Material(shad);
                foreach (var player in PlayerManager.instance.players)
                {
                    MakeObjectHidden(player);

                    MakeObjectHidden(player.data.weaponHandler.gun);
                    this.ExecuteAfterSeconds(0.5f, () =>
                    {
                        foreach (var sf in player.GetComponentsInChildren<SFPolygon>())
                        {
                            sf.enabled = false;
                        }
                        foreach (var sf in player.data.weaponHandler.gun.GetComponentsInChildren<SFPolygon>())
                        {
                            sf.enabled = false;
                        }
                    });
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

        public void SetCameraPosition(Vector3 pos, bool snap = false)
        {
            var mainCam = MainCam.instance.transform;
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
            foreach (var img in obj.GetComponentsInChildren<ProceduralImage>(true))
            {
                UnityEngine.Debug.Log(img.transform.root);
                if(img.transform.root.GetComponent<Player>().playerID == 0)//img.transform.root.GetComponent<PhotonView>() && !(img.transform.root.GetComponent<PhotonView>().IsMine) )
                {
                    var newMat = new Material(img.material);
                    newMat.name = img.name;
                    img.material = newMat;
                    newMat.SetFloat("_Stencil", 69);
                    newMat.SetFloat("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.Equal);
                }
            }


            var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomHidden"));
            foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }

            var procImgs = obj.GetComponentsInChildren<ProceduralImage>(true);
            foreach (var img in obj.GetComponentsInChildren<Image>(true))
            {
                if(img.material.shader == mat.shader || procImgs.Any(y => img.gameObject == y.gameObject)) continue;
                img.material = mat;
            }
            foreach (var renderer in obj.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }
            foreach (var renderer in obj.GetComponentsInChildren<LineRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }
            foreach (var renderer in obj.GetComponentsInChildren<TrailRenderer>(true))
            {
                ChangeMaterial(renderer, mat);
            }
            foreach (var renderer in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var newMat = new Material(renderer.fontMaterial);
                renderer.fontMaterial = newMat;
                newMat.SetFloat("_Stencil", 69);
                newMat.SetFloat("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.Equal);
            }

            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
            {
                ChangeMaterial(renderer, mat);
            }
        }

        private static void ChangeMaterial(Renderer renderer, Material hiddenMat)
        {
            var mats = renderer.materials;
            for (var i = 0; i < renderer.materials.Length; i++)
            {
                var mat = new Material(hiddenMat);
                if(renderer.materials[i].shader == mat.shader) continue;
                if (renderer.materials[i].name.Contains("Default"))
                {
                    mats[i] = mat;
                }
                else
                {
                    if(mats[i].HasProperty("_Color")) {
                        mat.color = mats[i].color;
                    }
                    var textureCopy = mats[i].mainTexture;
                    mats[i] = mat;
                    mats[i].mainTexture = textureCopy;
                }
            }
            renderer.materials = mats;
        }

        private void OnDestroy()
        {
            harmony.UnpatchAll(ModId);
        }
    }
}