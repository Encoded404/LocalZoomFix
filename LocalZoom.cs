using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using MapEmbiggener.Controllers;
using Photon.Pun;
using Sonigon;
using TMPro;
using UnboundLib;
using UnboundLib.Extensions;
using UnboundLib.Networking;
using UnboundLib.Utils;
using UnboundLib.Utils.UI;
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
        internal const string ModId = "com.bosssloth.rounds.LocalZoom";
        private const string ModName = "LocalZoom";
        public const string Version = "1.0.0";

        public static LocalZoom instance;

        public bool enableCamera = true;
        public bool enableResetCamera = false;
        public GameObject deathPortalBox;
        public GameObject phoenixCircle;
        public GameObject phoenixBlackBox;

        public static bool scaleCamWithBulletSpeed;

        internal static LayerMask extraLayerMask;

        private static ConfigEntry<bool> _enableCameraConfig;
        public static bool enableCameraSetting { get; private set; }
        
        private static ConfigEntry<bool> _enableShaderConfig;
        public static bool enableShaderSetting { get; private set; }

        private static AssetBundle shaderBundle;
        
        public static bool enableLoSNamePlates = false;

#if DEBUG
        internal const bool DEBUG = true;
#else
        internal const bool DEBUG = false;
#endif

        private Harmony harmony;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        
        public static bool IsInOfflineModeAndNotSandbox
        {
            get
            {
#if DEBUG
                return !(GM_Test.instance && GM_Test.instance.gameObject.activeSelf) && PhotonNetwork.OfflineMode;
#else
                return PhotonNetwork.OfflineMode;
#endif
            }
        }

        // Sources:
        // Sprite stencil shader (Modified) https://prime31.github.io/stencil-buffer-occlusion/
        // Sprites default shader https://github.com/nubick/unity-utils/blob/master/sources/Assets/Scripts/Shaders/Sprites-Default.shader
        // https://answers.unity.com/questions/590800/how-to-cullrender-to-through-a-window.html
        // https://www.youtube.com/watch?v=CSeUMTaNFYk

        //TODO: 
        // spectator camera when dead?
        // BUG weird color change of the jump particle
        // boxes don't use the particles anymore
        // damage indicator from side?
        // settings to customize the ViewSphere


        // looks like there is some unity bug with a sprite mask, all particles that are hidden by a mask
        // will always show up in any stencil buffer.


        private void Awake()
        {
            On.MainMenuHandler.Awake += (orig, self) =>
            {
                enableResetCamera = false;
                enableCamera = true;
                scaleCamWithBulletSpeed = false;
                orig(self);
            };
        }

        private void Start()
        {
            instance = this;
            
            harmony = new Harmony(ModId);
            harmony.PatchAll();

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
            _enableCameraConfig = Config.Bind("LocalZoom", "Enable Camera", true, "Enable the local camera");
            enableCameraSetting = _enableCameraConfig.Value;
            _enableShaderConfig = Config.Bind("LocalZoom", "Enable Shader", true, "Enable the local camera shader");
            enableShaderSetting = _enableShaderConfig.Value;
            
            ControllerManager.AddCameraController(MyCameraController.ControllerID, new MyCameraController());
            if (_enableCameraConfig.Value)
            {
                ControllerManager.SetCameraController(MyCameraController.ControllerID);
            }
            
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);
            
            Unbound.RegisterMenu("LocalZoom", () => { }, CreateUI, null);
        }

        private static void CreateUI(GameObject menu)
        {
            MenuHandler.CreateToggle(_enableCameraConfig.Value, "Enable camera", menu,
                value => 
                {
                    _enableCameraConfig.Value = value;
                    enableCameraSetting = value;
                    if (enableShaderSetting || enableCameraSetting)
                    {
                        ControllerManager.SetCameraController(MyCameraController.ControllerID);
                    }
                    else
                    {
                        ControllerManager.SetCameraController(ControllerManager.DefaultCameraControllerID);
                    }
                });
            
            MenuHandler.CreateToggle(_enableShaderConfig.Value, "Enable shader", menu,
                value => 
                {
                    _enableShaderConfig.Value = value;
                    enableShaderSetting = value;
                    if (enableShaderSetting || enableCameraSetting)
                    {
                        ControllerManager.SetCameraController(MyCameraController.ControllerID);
                    }
                    else
                    {
                        ControllerManager.SetCameraController(ControllerManager.DefaultCameraControllerID);
                    }
                });
        }

        void Update()
        {
            // If not in sandbox and in offlinemode return
            if (IsInOfflineModeAndNotSandbox || !LocalZoom.enableShaderSetting)
                return;

#if DEBUG
            if (Input.GetKeyDown(KeyCode.L))
            {
                MakeAllParticlesHidden();
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                GiveLocalPlayerViewCone(false);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                MakeAllPlayersHidden();
            }
#endif
        }

        public void MakeAllPlayersHidden()
        {
            if (IsInOfflineModeAndNotSandbox)
                return;
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
            if (IsInOfflineModeAndNotSandbox)
                return;
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
            if (IsInOfflineModeAndNotSandbox)
                return;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomParticleHidden"));
            mat.SetFloat("_RefLayer", 80);
            mat.SetTexture("_MainTex", renderer.material.GetTexture("_MainTex"));
            mat.SetColor("_Color", renderer.material.GetColor("_Color"));
            renderer.material = mat;
        }

        public void MakeParticleRendererPortal(GameObject obj)
        {
            if (IsInOfflineModeAndNotSandbox)
                return;
            Destroy(obj.GetComponent<SpriteMask>());
            var mat = new Material(shaderBundle.LoadAsset<Shader>("CustomParticlePortal"));
            mat.SetFloat("_RefLayer", 80);
            mat.color = new Color(0, 0, 0, 0);
            obj.GetComponent<SpriteRenderer>().material = mat;
        }

        public void GiveLocalPlayerViewCone(bool disable = true)
        {
            if (IsInOfflineModeAndNotSandbox)
                return;
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
            if (disable)
            {
                obj.SetActive(false);
            }

            var black = Instantiate(shaderBundle.LoadAsset<GameObject>("BlackBox"), player.transform);
            black.transform.localPosition = Vector3.zero;
            black.transform.localScale = Vector3.one * 1000f;
            var circle = Instantiate(shaderBundle.LoadAsset<GameObject>("PlayerCircle"), player.transform);
            circle.transform.localPosition = Vector3.zero;
            circle.transform.localScale = Vector3.one * 2.7f;
            if (disable)
            {
                circle.SetActive(false);
            }
                
            deathPortalBox = Instantiate(shaderBundle.LoadAsset<GameObject>("BlackBox"));
            deathPortalBox.transform.position = Vector3.zero;
            deathPortalBox.transform.localScale = Vector3.one * 1000f;
            deathPortalBox.GetComponent<Renderer>().material = new Material(shaderBundle.LoadAsset<Shader>("CustomPortal"));
            deathPortalBox.transform.SetZPosition(50);
            deathPortalBox.SetActive(false);
            
            phoenixCircle = Instantiate(shaderBundle.LoadAsset<GameObject>("PlayerCircle"));
            phoenixCircle.transform.position = Vector3.zero;
            phoenixCircle.transform.localScale = Vector3.one * 7.5f;
            phoenixCircle.SetActive(false);
            
            phoenixBlackBox = Instantiate(shaderBundle.LoadAsset<GameObject>("BlackBox"));
            phoenixBlackBox.transform.position = Vector3.zero;
            phoenixBlackBox.transform.localScale = Vector3.one * 100f;
            phoenixBlackBox.SetActive(false);
        }

        public void MakeAllParticlesHidden()
        {
            if (IsInOfflineModeAndNotSandbox)
                return;
            foreach (var renderer in MenuControllerHandler.instance.transform
                         .Find("Visual/Rendering /FrontParticles")
                         .GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                MakeParticleRendererHidden(renderer);
            }
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
                    if (img.transform.root.GetComponent<Player>())
                    {
                        condition = img.transform.root.GetComponent<Player>().playerID == 0;
                    }
                    else if(img.transform.root.GetComponent<SpawnedAttack>())
                    {
                        condition = img.transform.root.GetComponent<SpawnedAttack>().spawner.playerID == 0;
                    }
                    else
                    {
                        condition = true;
                    }
                }
                else
                {
                    if (img.transform.root.GetComponent<Player>())
                    {
                        condition = img.transform.root.GetComponent<PhotonView>() &&
                                    !(img.transform.root.GetComponent<PhotonView>().IsMine);

                    }
                    else if(img.transform.root.GetComponent<SpawnedAttack>())
                    {
                        condition = !img.transform.root.GetComponent<SpawnedAttack>().spawner.GetComponent<PhotonView>()
                            .IsMine;
                    }
                    else
                    {
                        condition = true;
                    }

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
                if (renderer.transform.root.GetComponent<PhotonView>() &&
                    renderer.transform.root.GetComponent<PhotonView>().IsMine)
                {
                    continue;
                }
                
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

        internal static void OnHandShakeCompleted()
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(LocalZoom), nameof(LocalZoom.SyncSettings), new object[] {_enableCameraConfig.Value, _enableShaderConfig.Value});
            }
        }

        [UnboundRPC]
        public static void SyncSettings(bool enableCam, bool enableShader)
        {
            LocalZoom.enableCameraSetting = enableCam;
            LocalZoom.enableShaderSetting = enableShader;
        }


        /// <summary>
        /// This will need to be called on a RPC on all players before OnGameStart is called
        /// </summary>
        /// <param name="enable"></param>
        public static void SetEnableCameraSetting(bool enable)
        {
            _enableCameraConfig.Value = enable;
            enableCameraSetting = enable;
            if (enableShaderSetting || enableCameraSetting)
            {
                ControllerManager.SetCameraController(MyCameraController.ControllerID);
            }
            else
            {
                ControllerManager.SetCameraController(ControllerManager.DefaultCameraControllerID);
            }
        }

        /// <summary>
        /// This will need to be called on a RPC on all players before OnGameStart is called
        /// </summary>
        /// <param name="enable"></param>
        public static void SetEnableShaderSetting(bool enable)
        {
            _enableShaderConfig.Value = enable;
            enableShaderSetting = enable;
            if (enableShaderSetting || enableCameraSetting)
            {
                ControllerManager.SetCameraController(MyCameraController.ControllerID);
            }
            else
            {
                ControllerManager.SetCameraController(ControllerManager.DefaultCameraControllerID);
            }
        }
        
        public static void AddIgnoreLayerToMask(int layer)
        {
            extraLayerMask |= 1 << layer;
        }
        
        public static void AddIgnoreLayerToMask(LayerMask layer)
        {
            extraLayerMask |= layer;
        }
        

        private void OnDestroy()
        {
            harmony.UnpatchAll(ModId);
        }
    }
}