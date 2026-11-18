using System.Collections.Generic;

namespace FakeMG.InputHints.Editor
{
    internal sealed class InputGlyphPack
    {
        public string DisplayName { get; }
        public string LibraryAssetName { get; }

        /// <summary>Specific gamepads before broad ones: the library picks the first set covering a device.</summary>
        public IReadOnlyList<InputGlyphFamily> Families { get; }

        public InputGlyphPack(string displayName, string libraryAssetName, IReadOnlyList<InputGlyphFamily> families)
        {
            DisplayName = displayName;
            LibraryAssetName = libraryAssetName;
            Families = families;
        }
    }
}
