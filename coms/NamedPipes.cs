// Decompiled with JetBrains decompiler
// Type: coms.NamedPipes
// Assembly: VelocityAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 71748206-4FD3-4A74-93D8-E83A809D0659
// Assembly location: C:\Users\Szebu\Downloads\Files-Velocity (1)\Files-Velocity\VelocityAPI.dll

using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace coms;

internal class NamedPipes
{
  public static string luapipename = "uoQcySKXSUxxJNpVQyatpHQwYoGfhcbh";

  [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool WaitNamedPipe(string name, int timeout);

  public static bool NamedPipeExist(string pipeName)
  {
    try
    {
      if (!NamedPipes.WaitNamedPipe("\\\\.\\pipe\\" + pipeName, 0))
      {
        switch (Marshal.GetLastWin32Error())
        {
          case 0:
            return false;
          case 2:
            return false;
        }
      }
      return true;
    }
    catch (Exception ex)
    {
      return false;
    }
  }

  public static void LuaPipe(string script, int pid)
  {
    if (!NamedPipes.NamedPipeExist($"{NamedPipes.luapipename}_{pid}"))
      return;
    new Thread((ThreadStart) (() =>
    {
      try
      {
        using (NamedPipeClientStream pipeClientStream = new NamedPipeClientStream(".", $"{NamedPipes.luapipename}_{pid}", PipeDirection.Out))
        {
          pipeClientStream.Connect();
          using (StreamWriter streamWriter = new StreamWriter((Stream) pipeClientStream, Encoding.Default, 999999))
          {
            streamWriter.Write(script);
            streamWriter.Dispose();
          }
          pipeClientStream.Dispose();
        }
      }
      catch (IOException ex)
      {
      }
      catch (Exception ex)
      {
      }
    })).Start();
  }
}
