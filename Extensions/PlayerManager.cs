using UnityEngine;
namespace LocalZoom.Extensions
{
    public static class PlayerManagerExtensions
    {
        // overload for CanSeePlayer to accept custom layermasks
        public static CanSeeInfo CanSeePlayer(this PlayerManager instance, Vector2 from, Player player, LayerMask layerMask)
        {
            CanSeeInfo canSeeInfo = new CanSeeInfo();
            canSeeInfo.canSee = true;
            canSeeInfo.distance = float.PositiveInfinity;
            if (!player)
            {
                canSeeInfo.canSee = false;
                return canSeeInfo;
            }
            RaycastHit2D[] array = Physics2D.RaycastAll(from, (player.data.playerVel.position - from).normalized, Vector2.Distance(from, player.data.playerVel.position), layerMask);
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].transform && !array[i].transform.root.GetComponent<SpawnedAttack>() && !array[i].transform.root.GetComponent<Player>() && array[i].distance < canSeeInfo.distance)
                {
                    canSeeInfo.canSee = false;
                    canSeeInfo.hitPoint = array[i].point;
                    canSeeInfo.distance = array[i].distance;
                }
            }
            return canSeeInfo;
        }
    }
}
