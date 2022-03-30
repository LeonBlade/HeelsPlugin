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

  public struct Quad
  {
    public ushort A;
    public ushort B;
    public ushort C;
    public ushort D;

    public Quad(ulong data)
    {
      A = (ushort)data;
      B = (ushort)(data >> 16);
      C = (ushort)(data >> 32);
      D = (ushort)(data >> 48);
    }

    public ulong ToUlong()
    {
      return (ulong)(A | (B << 16) | (C << 32) | (D << 48));
    }
  }
}
