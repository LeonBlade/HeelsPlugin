using System;
using System.Runtime.InteropServices;

namespace HeelsPlugin
{
  [StructLayout(LayoutKind.Sequential)]
  public struct Vector3
  {
    public float X;
    public float Y;
    public float Z;

    public Vector3(float x, float y, float z)
    {
      X = x;
      Y = y;
      Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 One => new(1, 1, 1);
  }

  public struct EquipItem
  {
    public ushort Main;
    public byte Variant;
    public byte Dye;

    public EquipItem(uint data)
    {
      Main = (ushort)data;
      Variant = (byte)(data >> 16);
      Dye = (byte)(data >> 24);
    }

    public uint ToUInt()
    {
      return (uint)(Main | (Variant << 16) | (Dye << 24));
    }

    public override string ToString()
    {
      return $"{Main}, {Variant}, {Dye}";
    }
  }

  [Flags]
  public enum Sexes : byte
  {
    Male = 1,
    Female = 2
  }

  [Flags]
  public enum Races : byte
  {
    Hyur = 1,
    Elezen = 2,
    Lalafell = 4,
    Miqote = 8,
    Roegadyn = 16,
    AuRa = 32,
    Viera = 64,
    Hrothgar = 128
  }
}
