using System;

namespace HeelsPlugin
{
  [Serializable]
  public class ConfigModel
  {
    public string Name;
    public short Model; // legacy
    public uint ModelMain;
    public float Offset;
    public bool Enabled;
    public Sexes SexFilter = (Sexes)255;
    public Races RaceFilter = (Races)255;

    public ConfigModel()
    {
      Name = "";
    }
  }
}
