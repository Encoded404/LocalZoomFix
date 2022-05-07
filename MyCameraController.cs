using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapEmbiggener.Controllers;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using LocalZoom.Extensions;
using UnityEngine.UI;

namespace LocalZoom
{
    public class MyCameraController : MapEmbiggener.Controllers.CameraController
    {
        public const float SpectatorMoveSpeed = 1f;
        public const float SpectatorZoomSpeed = 1f;

        public static string ControllerID => LocalZoom.ModId;
        private bool firstTime = true;
        public float? zoomLevel = null;
        public static float defaultZoomLevel = ControllerManager.DefaultZoom;
        public static bool allowZoomIn = false;
        private static readonly int StencilComp = Shader.PropertyToID("_StencilComp");
        public static bool wasActiveLastFrame = true;

        public float MaxZoom { get; private set; } = defaultZoomLevel;

        public override IEnumerator OnInitStart(IGameModeHandler gm)
        {
            allowZoomIn = false; // default value - can be modified by gamemodes after OnInitStart
            return base.OnInitStart(gm);
        }

        public override void OnUpdate()
        {
            // If not in sandbox and in offlinemode return
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
            {
                zoomLevel = null;
                ZoomTarget = null;
                return;
            }

            if (LocalZoom.enableCameraSetting)
            {
                if (firstTime)
                {
                    firstTime = false;
                    ZoomTarget = defaultZoomLevel/1.25f;
                    zoomLevel = ZoomTarget ?? defaultZoomLevel;
                    MaxZoom = zoomLevel ?? ControllerManager.Zoom;
                }

                if(GameManager.instance.battleOngoing && !CardChoice.instance.IsPicking && !LocalZoom.instance.enableResetCamera) 
                {
                    if (LocalZoom.instance.enableCamera)
                    {
                        wasActiveLastFrame = true;

                        var player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);

                        if (player == null || !player.data.isPlaying || player.data.dead)
                        {
                            // if the player is spectating, set enableResetCamera to true
                            LocalZoom.instance.enableResetCamera = true;
                            zoomLevel = null;
                            ZoomTarget = null;
                        }
                        else
                        {
                            // var gunTransform = player.data.weaponHandler.gun.transform.GetChild(0);
                            // SetCameraPosition(gunTransform.position + gunTransform.forward*1.5f);
                            var playerpos = player.data.transform.position;
                            playerpos.z = -100f;
                            PositionTarget = playerpos;

                            if (LocalZoom.DEBUG || allowZoomIn)
                            {
                                if (player.data.playerActions.PlayerZoom() != 0 && zoomLevel != null)
                                {
                                    zoomLevel = UnityEngine.Mathf.Clamp((float)zoomLevel + player.data.playerActions.PlayerZoom(), 1f, LocalZoom.DEBUG ? float.MaxValue : MaxZoom);
                                }
                            }

                            if (LocalZoom.enableLoSNamePlates)
                            {
                                foreach (var otherPlayer in PlayerManager.instance.players)
                                {
                                    var canSee =
                                        PlayerManager.instance.CanSeePlayer(player.data.transform.position, otherPlayer);
                                    if (canSee.canSee)
                                    {
                                        foreach (var renderer in otherPlayer.data.GetData().allWobbleImages)
                                        {
                                            renderer.material.SetFloat(StencilComp, 8);
                                            renderer.enabled = true;
                                        }
                                        otherPlayer.data.GetPlayerNamePlate().fontMaterial.SetFloat(StencilComp, 8);
                                        otherPlayer.data.GetPlayerNamePlate().enabled = true;
                                    }
                                    else
                                    {
                                        foreach (var renderer in otherPlayer.data.GetData().allWobbleImages)
                                        {
                                            renderer.material.SetFloat(StencilComp, 3);
                                            renderer.enabled = false;
                                        }
                                        otherPlayer.data.GetPlayerNamePlate().fontMaterial.SetFloat(StencilComp, 3);
                                        otherPlayer.data.GetPlayerNamePlate().enabled = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!CardChoice.instance.IsPicking && LocalZoom.instance.enableResetCamera)
            {
                if (LocalZoom.enableCameraSetting && wasActiveLastFrame)
                {
                    var zero = Vector3.zero;
                    zero.z = -100;
                    PositionTarget = zero;
                    
                    zoomLevel = null; // use default zoom level calculated by MapEmbiggener
                }
                else if (LocalZoom.enableCameraSetting && LocalZoom.instance.enableSpectatorCamera)
                {
                    // allow spectators to move and zoom the camera
                    Player player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);
                    zoomLevel = ControllerManager.Zoom; 

                    if (player is null)
                    {
                        // fallback controls (WASD / scroll wheel)
                        if (Input.GetKey(KeyCode.W))
                        {
                            PositionTarget += Vector3.up * Time.deltaTime * SpectatorMoveSpeed;
                        }
                        if (Input.GetKey(KeyCode.S))
                        {
                            PositionTarget -= Vector3.up * Time.deltaTime * SpectatorMoveSpeed;
                        }
                        if (Input.GetKey(KeyCode.A))
                        {
                            PositionTarget -= Vector3.right * Time.deltaTime * SpectatorMoveSpeed;
                        }
                        if (Input.GetKey(KeyCode.D))
                        {
                            PositionTarget += Vector3.right * Time.deltaTime * SpectatorMoveSpeed;
                        }
                        if (Input.mouseScrollDelta.y != 0f)
                        {
                            zoomLevel = UnityEngine.Mathf.Clamp((float)zoomLevel + Input.mouseScrollDelta.y * Time.deltaTime * SpectatorZoomSpeed, 1f, float.MaxValue);
                        }
                    }
                    else
                    {
                        // use playerActions
                        if (player.data.playerActions.PlayerZoom() != 0 && zoomLevel != null)
                        {
                            zoomLevel = UnityEngine.Mathf.Clamp((float)zoomLevel + player.data.playerActions.PlayerZoom(), 1f, float.MaxValue);
                        }
                        if (player.data.playerActions.Move != Vector2.zero)
                        {
                            PositionTarget += ((Vector3)player.data.playerActions.Move).x * Vector3.right * Time.deltaTime * ControllerManager.Zoom * SpectatorMoveSpeed;
                            PositionTarget += ((Vector3)player.data.playerActions.Move).y * Vector3.up * Time.deltaTime * ControllerManager.Zoom * SpectatorMoveSpeed;
                        }
                    }

                }

                if (LocalZoom.enableShaderSetting && LocalZoom.instance.deathPortalBox != null)
                {
                    LocalZoom.instance.deathPortalBox.SetActive(true);
                }

                wasActiveLastFrame = false;
            }
            else
            {
                if (LocalZoom.enableShaderSetting && LocalZoom.instance.deathPortalBox != null)
                {
                    LocalZoom.instance.deathPortalBox.SetActive(false);
                }
            }

            if (LocalZoom.enableCameraSetting)
            {
                ZoomTarget = zoomLevel;
            }
            
            
            base.OnUpdate();
        }

        public override IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            yield return base.OnRoundStart(gm);
        }


        public override IEnumerator OnRoundEnd(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                yield break;
            
            LocalZoom.instance.ExecuteAfterFrames(1, () =>
            {
                LocalZoom.instance.enableResetCamera = false;
            });
            yield return base.OnRoundEnd(gm);
        }

        public override IEnumerator OnPointStart(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                yield break;

            var player = PlayerManager.instance.players.First(p => p.data.view.IsMine);
            if (LocalZoom.enableShaderSetting)
            {
                player.transform.Find("BlackBox(Clone)").gameObject.SetActive(true);
            }
            
            LocalZoom.instance.ExecuteAfterSeconds(1f, () =>
            {
                ResetZoomLevel(player);

                if (LocalZoom.enableShaderSetting)
                {
                    // player.transform.Find("BlackBox").gameObject.SetActive(true);
                    player.transform.Find("PlayerCircle(Clone)").gameObject.SetActive(true);
                    player.transform.Find("ViewSphere").gameObject.SetActive(true);
                }

            });
            yield return base.OnPointStart(gm);
        }

        public void ResetZoomLevel(Player player)
        {
            if (!player.data.view.IsMine) return;
            
            if (!LocalZoom.scaleCamWithBulletSpeed)
            {
                zoomLevel = Mathf.Clamp(
                    defaultZoomLevel / 1.20f * (player.transform.localScale.x/1.15f), 0,
                    defaultZoomLevel + defaultZoomLevel / 4);
                
            }
            else
            {
                zoomLevel = Mathf.Clamp(
                    defaultZoomLevel / 1.20f * (player.data.weaponHandler.gun.projectileSpeed/3.5f) , 0,
                    defaultZoomLevel + defaultZoomLevel / 4);
            }

            MaxZoom = (float)zoomLevel;
        }

        public override IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                yield break;
            LocalZoom.instance.enableResetCamera = true;

            var player = PlayerManager.instance.players.First(p => p.data.view.IsMine);
            // player.transform.Find("BlackBox").gameObject.SetActive(false);
            if (LocalZoom.enableShaderSetting)
            {
                player.transform.Find("PlayerCircle(Clone)").gameObject.SetActive(false);
                player.transform.Find("ViewSphere").gameObject.SetActive(false);
            }
            
            yield return base.OnPointEnd(gm);
        }

        public override IEnumerator OnPickStart(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                yield break;
            if (LocalZoom.enableShaderSetting)
            {
                LocalZoom.instance.enableCamera = false;
                LocalZoom.instance.enableResetCamera = false;
                
                LocalZoom.instance.ExecuteAfterSeconds(5.5f, () =>
                {
                    var player = PlayerManager.instance.players.First(p => p.data.view.IsMine);
                    player.transform.Find("BlackBox(Clone)").gameObject.SetActive(true);
                });
            }
            yield return base.OnPickStart(gm);
        }

        public override IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                yield break;
            LocalZoom.instance.StartCoroutine(DisableCameraTemp());
            LocalZoom.instance.enableResetCamera = true;
            
            yield return base.OnPickEnd(gm);
        }
        
        public static IEnumerator DisableCameraTemp()
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || !LocalZoom.enableCameraSetting)
                yield break;
            LocalZoom.instance.enableCamera = false;
            yield return new WaitForSeconds(1);
            LocalZoom.instance.enableCamera = true;
        }
        
        public override IEnumerator OnGameStart(IGameModeHandler gm)
        {
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                yield break;

            if (LocalZoom.enableCameraSetting)
            {
                defaultZoomLevel = ControllerManager.DefaultZoom;
                zoomLevel = defaultZoomLevel;
            }

            if (LocalZoom.enableShaderSetting)
            {
                LocalZoom.instance.ExecuteAfterSeconds(5, () =>
                {
                    UnityEngine.Debug.Log("players hidden");
                    LocalZoom.instance.MakeAllParticlesHidden();
                    LocalZoom.instance.MakeAllPlayersHidden();
                    LocalZoom.instance.GiveLocalPlayerViewCone();
                });
            }
            yield return base.OnGameStart(gm);
        }


        public override void SetDataToSync()
        {
        }

        public override void ReadSyncedData()
        {
        }

        public override bool SyncDataNow()
        {
            return false;
        }
    }
}