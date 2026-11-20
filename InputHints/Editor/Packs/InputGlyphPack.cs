using System;
using System.Collections.Generic;

namespace FakeMG.InputHints.Editor
{
    internal sealed class InputGlyphPack
    {
        public string DisplayName { get; }
        public string LibraryAssetName { get; }

        /// <summary>Specific gamepads before broad ones: the library picks the first set covering a device.</summary>
        public IReadOnlyList<InputGlyphFamily> Families { get; }

        /// <summary>
        /// Generates art the pack does not ship, such as a diagram, into the textures folder before the sets are built.
        /// Receives the textures folder. Null when the pack ships everything.
        /// </summary>
        public Action<string> PrepareTexturesFolder { get; }

        public InputGlyphPack(
            string displayName,
            string libraryAssetName,
            IReadOnlyList<InputGlyphFamily> families,
            Action<string> prepareTexturesFolder = null)
        {
            DisplayName = displayName;
            LibraryAssetName = libraryAssetName;
            Families = families;
            PrepareTexturesFolder = prepareTexturesFolder;
        }
    }
}
