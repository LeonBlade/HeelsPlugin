using System;

namespace HeelsPlugin
{
  [Serializable]
  public class ConfigModel
  {
    public string Name;
    public short Model;
    public float Offset;
    public bool Enabled;
  }
}
