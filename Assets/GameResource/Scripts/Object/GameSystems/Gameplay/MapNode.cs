using System;

namespace Backend.Object.GameSystems.Gameplay
{
    /// <summary>
    /// 맵 노드 런타임 데이터. 시드 생성 결과이며 런 도중 구조는 변하지 않는다.
    /// </summary>
    [Serializable]
    public struct MapNode
    {
        public int Floor;
        public int Slot;
        public MapNodeType NodeType;
        public string ContentId;
        public int[] NextSlots;
        public MapNodeState State;

        public (int floor, int slot) Key => (Floor, Slot);
    }
}
