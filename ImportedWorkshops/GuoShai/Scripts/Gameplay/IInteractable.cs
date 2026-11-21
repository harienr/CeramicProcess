using UnityEngine;

namespace Porcelain
{
    /// <summary>玩家可以“看”和“操作”的东西。提示文字短而具体（规范 §7）。</summary>
    public interface IInteractable
    {
        Transform Point { get; }
        float Radius { get; }

        /// <summary>当前应显示的提示；返回 null 表示此刻不可操作</summary>
        string Prompt { get; }

        /// <summary>按下 / 按住时调用</summary>
        void Interact(float dt, bool pressed, bool held);

        /// <summary>是否仍在被使用（用于锁定提示与镜头）</summary>
        bool Busy { get; }
    }
}
