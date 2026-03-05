all settings of the game are stored in a single file, with different sections for each tab
there are many settings. we don't know how many settings there will be in the future, so we need a flexible way to store them

----------------------------------------

each tab has some common settings for all games, and some specific settings for each game
data class
    - all settings of a tab are in the same class
    - each setting has a class for the data
      - extended from a base class: GameGraphicsSettingsData extended CommonGraphicsSettingsData
      - common settings are stored in 1 class, specific settings are stored in another separate class

----------------------------------------

How will users add settings to the game?
    Drag a prefab (slider, option) into the scene for each setting
    Assign the setting data (min max value / options) to the prefab
    Write the logic that will read the current value of the setting and update the system accordingly (e.g. change the resolution, change the language, etc.)

Design
    Each setting has its own id
    Clicking the UI will change the value of the setting that is stored in the data manager
    The systems that use the settings will listen to the value from the data manager and update accordingly
    Different data type for each setting (could be any primitive type, or even a custom class)
no predefined amount of settings, can be added or removed in the future

How are settings added to the data manager?
The data manager for each tab will store a dictionary of setting id and setting data
    but the data is different for each setting

file
    individual
    group
        individual
        group
    (predefined or no predefined amount)

----------------------------------------

// Connect the logic
//      UI to the actual setting logic
//      update the UI when the setting data is changed
// The actual setting logic
// Save and Load settings

// Game (language, depending on the game)
// Graphics (preset, lighting quality, shadow quality, shadow resolution, texture quality, anti-aliasing, post-processing effects, depending on the game)
// Controls (keybindings, controller sensitivity, invert axis, depending on the game.)

Default values for graphics settings:
presetIndex = 2, // Default to Medium preset
lightingQualityIndex = 1, // Default to Medium lighting quality
shadowQualityIndex = 2, // Default to Medium shadow quality
shadowResolutionIndex = 1, // Default to Medium shadow resolution
textureQualityIndex = 0, // Default to Full Res texture quality
antiAliasingIndex = 0, // Default to Off anti-aliasing
postProcessingIndex = 1 // Default to On post-processing