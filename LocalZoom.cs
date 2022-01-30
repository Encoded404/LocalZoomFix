﻿using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using TMPro;
using UnboundLib;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnityEngine;
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
        
        public static bool IsInOfflineModeAndNotSandbox => !(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode;

        // Sources:
        // Sprite stencil shader (Modified) https://prime31.github.io/stencil-buffer-occlusion/
        // Sprites default shader https://github.com/nubick/unity-utils/blob/master/sources/Assets/Scripts/Shaders/Sprites-Default.shader
        // https://answers.unity.com/questions/590800/how-to-cullrender-to-through-a-window.html
        // https://www.youtube.com/watch?v=CSeUMTaNFYk

        //TODO: 
        // spectator camera when dead?
        // BUG weird color change of the jump particle


        // looks like there is some bug with a sprite mask, all particles that are hidden by a mask
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
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, Numerators.PointEnd);

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
            // If not in sandbox and in offlinemode return
            if (IsInOfflineModeAndNotSandbox)
                return;


            if(GameManager.instance.battleOngoing && !CardChoice.instance.IsPicking && !enableResetCamera) {
                if (enableCamera)
                {
                    var player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);

                    if (player == null || !player.data.isPlaying) return;

                    var gunTransform = player.data.weaponHandler.gun.transform.GetChild(0);
                    // SetCameraPosition(gunTransform.position + gunTransform.forward*1.5f);
                    SetCameraPosition(player.transform.position);

                    
#if DEBUG
                    if (Input.mouseScrollDelta.y > 0)
                    {
                        // zoom in
                        MapManager.instance.currentMap.Map.size -= 1f;
                    } else if (Input.mouseScrollDelta.y < 0)
                    {
                        // zoom out
                        MapManager.instance.currentMap.Map.size += 1f;
                    }
#endif
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

#if DEBUG
            if (Input.GetKeyDown(KeyCode.L))
            {
                MakeAllParticlesHidden();
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                GiveLocalPlayerViewCone();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                MakeAllPlayersHidden();
            }
#endif
        }

        public void MakeAllPlayersHidden()
        {
            foreach (var player in PlayerManager.instance.players)
            {
                MakeObjectHidden(player);

                this.ExecuteAfterSeconds(0.1f, () =>
                {
                    MakeGunHidden(player);
                });

                // Make particles hidden
                MakeParticleRendererHidden(player.transform.Find("PlayerSkin/Skin_PlayerOne(Clone)")
                    .GetComponent<ParticleSystemRenderer>());

            }
        }

        public void MakeGunHidden(Player player)
        {
            MakeObjectHidden(player.data.weaponHandler.gun);
            foreach (var sf in player.GetComponentsInChildren<SFPolygon>(true))
            {
                sf.enabled = false;
            }
            foreach (var sf in player.data.weaponHandler.gun.GetComponentsInChildren<SFPolygon>(true))
            {
                Destroy(sf);
            }
            foreach (var mask in player.data.weaponHandler.gun.GetComponentsInChildren<SpriteMask>(true))
            {
                mask.GetComponent<SetTeamColor>().enabled = false;
                mask.GetComponent<SpriteRenderer>().enabled = true;
                mask.GetComponent<SpriteRenderer>().color = ExtraPlayerSkins.GetPlayerSkinColors(player.colorID()).color;
                mask.enabled = false;
            }
        }

        public void MakeParticleRendererHidden(ParticleSystemRenderer renderer)
        {
            renderer.maskInteraction = SpriteMaskInteraction.None;
            var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomParticleHidden"));
            mat.SetInt("_RefLayer", 80);
            mat.SetTexture("_MainTex", renderer.material.GetTexture("_MainTex"));
            mat.SetColor("_Color", renderer.material.GetColor("_Color"));
            renderer.material = mat;
        }

        public void MakeParticleRendererPortal(GameObject obj)
        {
            obj.GetComponent<SpriteMask>().enabled = false;
            var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomParticlePortal"));
            mat.SetInt("_RefLayer", 80);
            mat.color = new Color(0, 0, 0, 0);
            obj.GetComponent<SpriteRenderer>().material = mat;
        }

        public void GiveLocalPlayerViewCone()
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

        public void MakeAllParticlesHidden()
        {
            foreach (var renderer in MenuControllerHandler.instance.transform
                         .Find("Visual/Rendering /FrontParticles")
                         .GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                MakeParticleRendererHidden(renderer);
            }
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
        }

        public static void MakeObjectHidden(Component obj)
        {
            if (IsInOfflineModeAndNotSandbox)
                return;
            foreach (var img in obj.GetComponentsInChildren<ProceduralImage>(true))
            {
                bool condition;
                if (PhotonNetwork.OfflineMode)
                {
                    condition = img.transform.root.GetComponent<Player>().playerID == 0;
                }
                else
                {
                    condition = img.transform.root.GetComponent<PhotonView>() &&
                                !(img.transform.root.GetComponent<PhotonView>().IsMine);
                }
                if(condition)
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

            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>(true))
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

                    if (mats[i].HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", mats[i].GetTexture("_MainTex"));
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