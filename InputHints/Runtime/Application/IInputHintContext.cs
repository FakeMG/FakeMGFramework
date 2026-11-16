using System.Collections.Generic;
using FakeMG.InputContexts;

namespace FakeMG.InputHints
{
    public interface IInputHintContext
    {
        IInputContext InputContext { get; }

        /// <remarks>Fixed once added; change what the player sees with <see cref="InputHint.SetLabel"/> and <see cref="InputHint.SetShown"/>.</remarks>
        IReadOnlyList<InputHint> Hints { get; }
    }
}
