namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines the stable output identifiers used by the supplied environment profile and applicators.
    /// </summary>
    public static class DayNightEnvironmentOutputIds
    {
        public const string MAIN_LIGHT_ENABLED = "main_light_enabled";
        public const string MAIN_LIGHT_ROTATION = "main_light_rotation";
        public const string MAIN_LIGHT_COLOR = "main_light_color";
        public const string MAIN_LIGHT_INTENSITY = "main_light_intensity";
        public const string MAIN_LIGHT_SHADOW_STRENGTH = "main_light_shadow_strength";

        public const string SKY_SUN_DISK = "sky_sun_disk";
        public const string SKY_SUN_SIZE = "sky_sun_size";
        public const string SKY_SUN_SIZE_CONVERGENCE = "sky_sun_size_convergence";
        public const string SKY_ATMOSPHERE_THICKNESS = "sky_atmosphere_thickness";
        public const string SKY_TINT = "sky_tint";
        public const string SKY_GROUND_COLOR = "sky_ground_color";
        public const string SKY_EXPOSURE = "sky_exposure";

        public const string FOG_ENABLED = "fog_enabled";
        public const string FOG_MODE = "fog_mode";
        public const string FOG_COLOR = "fog_color";
        public const string FOG_DENSITY = "fog_density";
        public const string FOG_START_DISTANCE_METERS = "fog_start_distance_meters";
        public const string FOG_END_DISTANCE_METERS = "fog_end_distance_meters";

        public const string AMBIENT_MODE = "ambient_mode";
        public const string AMBIENT_INTENSITY = "ambient_intensity";
        public const string AMBIENT_FLAT_COLOR = "ambient_flat_color";
        public const string AMBIENT_SKY_COLOR = "ambient_sky_color";
        public const string AMBIENT_EQUATOR_COLOR = "ambient_equator_color";
        public const string AMBIENT_GROUND_COLOR = "ambient_ground_color";
    }
}
