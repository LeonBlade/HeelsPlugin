using System;

namespace HeelsPlugin
{
  [Serializable]
  public class ConfigModel
  {
    public string Name;
    public short Model; // legacy
    public ulong ModelMain;
    public float Offset;
    public bool Enabled;
  }
}
