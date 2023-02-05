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

                            // if the player is spectating show all player components
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

                        }

                        this.HandleExtraOptions(player);

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

        private void HandleExtraOptions(Player localPlayer)
        {

            bool isSpectating = localPlayer is null || localPlayer.data.dead || !localPlayer.data.isPlaying;

            if (LocalZoom.enableLoSNamePlates || LocalZoom.disableLimbClipping)
            {
                ViewSphere viewSphere = localPlayer.GetComponentInChildren<ViewSphere>(true);
                float viewDistance = viewSphere?.viewDistance ?? float.MaxValue;
                float fov = viewSphere?.fov ?? float.MaxValue;

                foreach (var otherPlayer in PlayerManager.instance.players.Where(p => !p.data.dead && (isSpectating || p.playerID != localPlayer.playerID)))
                {
                    bool canSeeOther = true;

                    if (!isSpectating)
                    {
                        // check that the other player is within the viewsphere
                        if (Vector2.Distance(localPlayer.data.transform.position, otherPlayer.data.transform.position) > viewDistance
                            || Vector2.Angle(localPlayer.data.weaponHandler.gun.shootPosition.forward, otherPlayer.data.transform.position - localPlayer.data.transform.position) > fov / 2)
                        {
                            // if not, then the player is not within LoS
                            canSeeOther = false;
                        }

                        if (canSeeOther)
                        {
                            // now check if the other player is NOT obscured
                            canSeeOther = PlayerManager.instance.CanSeePlayer(localPlayer.data.transform.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee;
                        }
                    }
                    if (LocalZoom.enableLoSNamePlates)
                    {
                        if (canSeeOther)
                        {
                            // if the other player is withinLoS then enable the wobble images
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
                            // otherwise, disable the wobble images
                            foreach (var renderer in otherPlayer.data.GetData().allWobbleImages)
                            {
                                renderer.material.SetFloat(StencilComp, 3);
                                renderer.enabled = false;
                            }
                            otherPlayer.data.GetPlayerNamePlate().fontMaterial.SetFloat(StencilComp, 3);
                            otherPlayer.data.GetPlayerNamePlate().enabled = false;
                        }
                    }

                    if (LocalZoom.disableLimbClipping)
                    {

                        // hide weapons, arms, legs, and the shield stone if their own player cannot see them

                        // if the current player has LoS to this player, then skip this and just enable everything

                        // check that the owner can see their stone (i.e. it isn't poking through a wall)
                        Transform shieldStone = otherPlayer.transform.Find("Limbs/ArmStuff/ShieldStone");
                        if (shieldStone != null)
                        {
                            bool canSeeStone = canSeeOther || PlayerManager.instance.CanSeePlayer(shieldStone.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee;
                            foreach (var graphic in shieldStone.GetComponentsInChildren<SpriteRenderer>(true))
                            {
                                graphic.enabled = canSeeStone;
                            }
                            foreach (var image in shieldStone.GetComponentsInChildren<Image>(true))
                            {

                                image.enabled = canSeeStone;
                            }
                        }

                        // check that the owner can see their weapon (i.e. it isn't poking through a wall)
                        Transform weapon = otherPlayer.data.weaponHandler.gun.transform;
                        if (weapon != null)
                        {
                            bool canSeeWeapon = canSeeOther || PlayerManager.instance.CanSeePlayer(weapon.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee;
                            foreach (var graphic in weapon.GetComponentsInChildren<SpriteRenderer>(true))
                            {
                                graphic.enabled = canSeeWeapon;
                            }
                            foreach (var image in weapon.GetComponentsInChildren<Image>(true))
                            {
                                // special case of ignoring unused object
                                if (image.name == "Image" && image.GetComponent<GridLayoutGroup>() != null)
                                {
                                    continue;
                                }

                                image.enabled = canSeeWeapon;
                            }
                        }

                        // show only the parts of the owner's left leg that they can see 
                        Transform leftLeg = otherPlayer.transform.Find("Limbs/LegStuff/LegRendererLeft");
                        if (leftLeg != null)
                        {
                            // check the joints in order of closest to farthest, if the player can't see any one of them, then everything past that is automatically hidden
                            //Transform[] joints = leftLeg.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - otherPlayer.transform.position).sqrMagnitude).ToArray();
                            Transform[] joints = otherPlayer.data.GetSortedLegJoints(true);
                            if (canSeeOther)
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(false);
                                }
                                foreach (Transform joint in joints)
                                {
                                    if (!PlayerManager.instance.CanSeePlayer(joint.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee)
                                    {
                                        break;
                                    }
                                    joint.gameObject.SetActive(true);
                                }
                            }
                        }
                        // show only the parts of the owner's right leg that they can see 
                        Transform rightLeg = otherPlayer.transform.Find("Limbs/LegStuff/LegRendererRight");
                        if (rightLeg != null)
                        {
                            // check the joints in order of closest to farthest, if the player can't see any one of them, then everything past that is automatically hidden
                            //Transform[] joints = rightLeg.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - otherPlayer.transform.position).sqrMagnitude).ToArray();
                            Transform[] joints = otherPlayer.data.GetSortedLegJoints(false);
                            if (canSeeOther)
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(false);
                                }
                                foreach (Transform joint in joints)
                                {
                                    if (!PlayerManager.instance.CanSeePlayer(joint.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee)
                                    {
                                        break;
                                    }
                                    joint.gameObject.SetActive(true);
                                }
                            }
                        }
                        // show only the parts of the owner's left arm that they can see 
                        Transform leftArm = otherPlayer.transform.Find("Limbs/ArmStuff/ArmRendererLeft");
                        if (leftArm != null)
                        {
                            // check the joints in order of closest to farthest, if the player can't see any one of them, then everything past that is automatically hidden
                            //Transform[] joints = leftArm.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - otherPlayer.transform.position).sqrMagnitude).ToArray();
                            Transform[] joints = otherPlayer.data.GetSortedArmJoints(true);
                            if (canSeeOther)
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(false);
                                }
                                foreach (Transform joint in joints)
                                {
                                    if (!PlayerManager.instance.CanSeePlayer(joint.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee)
                                    {
                                        break;
                                    }
                                    joint.gameObject.SetActive(true);
                                }
                            }
                        }
                        // show only the parts of the owner's right arm that they can see 
                        Transform rightArm = otherPlayer.transform.Find("Limbs/ArmStuff/ArmRendererRight");
                        if (rightArm != null)
                        {
                            // check the joints in order of closest to farthest, if the player can't see any one of them, then everything past that is automatically hidden
                            //Transform[] joints = rightArm.GetComponentsInChildren<Transform>(true).Where(j => j.name == "Joint(Clone)").OrderBy(j => (j.position - otherPlayer.transform.position).sqrMagnitude).ToArray();
                            Transform[] joints = otherPlayer.data.GetSortedArmJoints(false);
                            if (canSeeOther)
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform joint in joints)
                                {
                                    joint.gameObject.SetActive(false);
                                }
                                foreach (Transform joint in joints)
                                {
                                    if (!PlayerManager.instance.CanSeePlayer(joint.position, otherPlayer, PlayerManager.instance.canSeePlayerMask | LayerMask.GetMask("Corpse")).canSee)
                                    {
                                        break;
                                    }
                                    joint.gameObject.SetActive(true);
                                }
                            }
                        }
                    }
                }
            }
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
                    player.transform.Find("PlayerCircle(Clone)").gameObject.SetActive(LocalZoom.enablePlayerCircles);
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