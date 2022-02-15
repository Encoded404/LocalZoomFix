using System.Collections;
using System.Linq;
using MapEmbiggener.Controllers;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace LocalZoom
{
    public class MyCameraController : MapEmbiggener.Controllers.CameraController
    {
        private bool firstTime = true;
        public float zoomLevel;

        public override void OnUpdate()
        {
            // If not in sandbox and in offlinemode return
            if (LocalZoom.IsInOfflineModeAndNotSandbox || (!LocalZoom.enableCameraSetting && !LocalZoom.enableShaderSetting))
                return;

            if (LocalZoom.enableCameraSetting)
            {
                if (firstTime)
                {
                    firstTime = false;
                    ZoomTarget = ControllerManager.DefaultZoom/1.25f;
                    zoomLevel = ZoomTarget ?? ControllerManager.DefaultZoom;
                }

                if(GameManager.instance.battleOngoing && !CardChoice.instance.IsPicking && !LocalZoom.instance.enableResetCamera) 
                {
                    if (LocalZoom.instance.enableCamera)
                    {
                        var player = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);

                        if (player == null || !player.data.isPlaying) return;

                        // var gunTransform = player.data.weaponHandler.gun.transform.GetChild(0);
                        // SetCameraPosition(gunTransform.position + gunTransform.forward*1.5f);
                        var playerpos = player.data.transform.position;
                        playerpos.z = -100f;
                        PositionTarget = playerpos;

    #if DEBUG
                        if (Input.mouseScrollDelta.y > 0)
                        {
                            // zoom in
                            zoomLevel -= 1f;
                        } else if (Input.mouseScrollDelta.y < 0)
                        {
                            // zoom out
                            zoomLevel += 1f;
                        }
    #endif
                    }
                }
            }

            if (!CardChoice.instance.IsPicking && LocalZoom.instance.enableResetCamera)
            {
                if (LocalZoom.enableCameraSetting)
                {
                    var zero = Vector3.zero;
                    zero.z = -100;
                    PositionTarget = zero;
                    
                    zoomLevel = ControllerManager.DefaultZoom;
                }

                if (LocalZoom.enableShaderSetting && LocalZoom.instance.deathPortalBox != null)
                {
                    LocalZoom.instance.deathPortalBox.SetActive(true);
                }
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
            if (!LocalZoom.scaleCamWithBulletSpeed)
            {
                zoomLevel = Mathf.Clamp(
                    ControllerManager.DefaultZoom / 1.20f * (player.transform.localScale.x/1.15f), 0,
                    ControllerManager.DefaultZoom + ControllerManager.DefaultZoom / 4);
                
            }
            else
            {
                zoomLevel = Mathf.Clamp(
                    ControllerManager.DefaultZoom / 1.20f * (player.data.weaponHandler.gun.projectileSpeed/3.5f) , 0,
                    ControllerManager.DefaultZoom + ControllerManager.DefaultZoom / 4);
            }
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
                zoomLevel = ControllerManager.DefaultZoom;
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