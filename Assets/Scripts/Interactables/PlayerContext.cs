using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 상호작용 대상에게 넘기는 플레이어 핸들. InteractionSensor가 1회 생성해 재사용한다.
    /// </summary>
    public sealed class PlayerContext
    {
        public PlayerManager Player { get; }
        public Transform Transform { get; }

        public PlayerContext(PlayerManager player)
        {
            Player = player;
            Transform = player.transform;
        }
    }
}
