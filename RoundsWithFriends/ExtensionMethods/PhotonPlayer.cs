namespace RWF
{
    public static class PhotonPlayerExtensions
    {
        public static void SetProperty(this Photon.Realtime.Player instance, string key, object value)
        {
            var propKey = RWFMod.GetCustomPropertyKey(key);
            var props = instance.CustomProperties;

            if (!props.ContainsKey(propKey))
            {
                props.Add(propKey, value);
            }
            else
            {
                props[propKey] = value;
            }

            instance.SetCustomProperties(props);
        }

        public static void SetModded(this Photon.Realtime.Player instance)
        {
            instance.SetProperty("modded", true);
        }
    }
}
